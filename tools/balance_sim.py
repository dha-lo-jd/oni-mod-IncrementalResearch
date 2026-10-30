"""Balance simulator for the idle game (best-play model). Not shipped with the mod.

Mirrors Core/KnowledgeRules.cs and Core/KnowledgeState.cs: greedy purchases, a permanent unlock of tiers 2 and 3
and a shift policy "shift when the gain reaches rho * max(Meta-Insight earned, 1/k)". Use it to retune the
constants. See docs/balance.md.

Run:  python tools/balance_sim.py [base_rate] [hours]
"""
import math
import sys


class Params:
    S = 20.0
    mi_thr = 1e5
    mi_exp = 0.5
    mi_k0 = 0.1
    mi_kper = 0.02
    mi_cost0 = 3.0
    mi_growth = 1.5
    bonus2 = 0.002
    bonus3 = 0.001
    # hooks overridden by variants
    gain = None        # f(T) -> MI gain (float, floor applied by caller)
    mult = None        # f(Mearned, scaleMi) -> multiplier
    bonus_uses_mi = True
    rho = 1.0
    Sref = 8.0
    gamma = 1.0

def default_gain(P, T):
    x = T / P.mi_thr
    return math.floor(x ** P.mi_exp + 1e-9) if x >= 1 else 0

def default_mult(P, M, smi):
    return 1 + (P.mi_k0 + P.mi_kper * smi) * M

# upgrades: id, tier(0 global), cost, effect, value, reqexp, prereq, currency, reqshifts, persists
U = [
 ("T1_INDEX",1,50,"mul",2,3,None,"I",0,False),
 ("T1_CLASSIFY",1,1000,"mul",2,15,"T1_INDEX","I",0,False),
 ("T1_CROSSREF",1,20000,"mul",3,30,"T1_CLASSIFY","I",0,False),
 ("T1_SHELVING",1,500,"grow",1.13,10,None,"I",0,False),
 ("G_NETWORK",0,5000,"mul",1.5,20,None,"I",0,False),
 ("T2_PARALLEL",2,50000,"mul",2,5,None,"I",0,False),
 ("T2_PIPELINE",2,2e6,"mul",2,15,"T2_PARALLEL","I",0,False),
 ("T2_COOLING",2,5e5,"grow",1.16,10,None,"I",0,False),
 ("T3_ISOLATION",3,5e9,"mul",2,5,None,"I",0,False),
 ("T3_CHAMBER",3,5e10,"grow",1.20,10,None,"I",0,False),
 ("T1_AUTO_EXPAND",1,2000,"autoexp",0,12,None,"I",0,True),
 ("M_HEADSTART",0,3,"start",100,0,None,"M",1,True),
 ("M_DISCOUNT",1,8,"disc",0.5,0,None,"M",2,True),
]
BASE = {1:10.0, 2:1000.0, 3:1e6}
GROW = {1:1.15, 2:1.18, 3:1.22}

def run(P, hours=100, dt=2.0, verbose=True, label=""):
    gain = P.gain or default_gain
    multf = P.mult or default_mult
    t = 0.0
    ex = {1:0,2:0,3:0}
    ups = set(); mups = set()
    know = 0.0; T = 0.0
    Mearned = 0; Mspent = 0; smi = 0; shifts = 0
    t2 = t3 = False
    b1 = b2 = 0.0
    rows = []
    cycle_start = 0.0
    peakP = 0.0
    know = 0.0
    inf = False
    end = hours * 3600
    def has(i): return i in ups or i in mups
    def promul(tier):
        r = 1.0
        for u in U:
            if u[3]=="mul" and (u[1]==tier or u[1]==0) and has(u[0]): r *= u[4]
        return r
    def growth(tier):
        g = GROW[tier]
        for u in U:
            if u[3]=="grow" and u[1]==tier and has(u[0]): g = min(g,u[4])
        return g
    def factor(tier):
        f = 1.0
        for u in U:
            if u[3]=="disc" and u[1]==tier and has(u[0]): f *= u[4]
        return f
    def cost(tier): return BASE[tier]*factor(tier)*growth(tier)**ex[tier]
    def vis(u):
        if has(u[0]): return False
        sc = ex[1] if u[1]==0 else ex[u[1]]
        if u[1]!=0 and not (t2 if u[1]==2 else t3 if u[1]==3 else True): return False
        return sc>=u[5] and shifts>=u[8] and (u[6] is None or has(u[6]))
    def start_know():
        return 100.0 if has("M_HEADSTART") else 0.0
    know = start_know()
    while t < end:
        Seff = P.Sref*(P.S/P.Sref)**P.gamma
        mi = multf(P, Mearned, smi)
        n1,n2,n3 = ex[1]+1, ex[2]+1, ex[3]+1
        mu1 = promul(1)*mi; mu2=promul(2)*mi; mu3=promul(3)*mi
        bmi2 = mu2 if P.bonus_uses_mi else promul(2)
        bmi3 = mu3 if P.bonus_uses_mi else promul(3)
        E1 = 1+b1; E2 = 1+b2
        prod = Seff*n1*mu1*E1
        if math.isinf(prod) or prod>1e300:
            inf=True; break
        know += prod*dt; T += prod*dt
        peakP = prod
        if t2: b1 += dt*P.bonus2*Seff*n2*bmi2*E2
        if t3: b2 += dt*P.bonus3*Seff*n3*bmi3
        # purchases (greedy cheapest first)
        while True:
            opts = []
            for u in U:
                if u[7]=="I" and vis(u) and u[2]<=know: opts.append((u[2],"u",u))
            for tier in (1,2,3):
                if tier==2 and not t2: continue
                if tier==3 and not t3: continue
                if tier==1 or True:
                    c = cost(tier)
                    if c<=know and (tier!=1 or True): opts.append((c,"e",tier))
            if not opts: break
            opts.sort(key=lambda o:o[0])
            c,k,x = opts[0]
            know -= c
            if k=="u": ups.add(x[0])
            else: ex[x]+=1
            if not t2 and ex[1]>=25: t2=True
            if not t3 and ex[2]>=15: t3=True
        # shift decision
        g = gain(P, T)
        cyc = t-cycle_start
        if g>=1 and g >= max(1, P.rho*max(Mearned, getattr(P,'mmin',0))):
            rows.append((t/3600, cyc/60, shifts, Mearned, g, peakP, mi))
            Mearned += g; shifts += 1
            # MI purchases
            bal = Mearned-Mspent
            while True:
                bought=False
                for u in U:
                    if u[7]=="M" and not has(u[0]) and shifts>=u[8] and u[2]<=bal:
                        mups.add(u[0]); bal-=u[2]; Mspent+=u[2]; bought=True
                c = P.mi_cost0*P.mi_growth**smi
                if c<=bal:
                    smi+=1; bal-=c; Mspent+=c; bought=True
                if not bought: break
            ex = {1:0,2:0,3:0}
            ups = set(u for u in ups if next(x for x in U if x[0]==u)[9])
            b1=b2=0.0; T=0.0; know=start_know(); cycle_start=t
        t += dt
    if verbose:
        print("==",label,"S=",P.S,"inf" if inf else "", "t_end(h)=",round(t/3600,2),"shifts",shifts)
        step = max(1,len(rows)//25)
        for r in rows[::step]:
            print("t=%6.2fh cyc=%7.1fmin shift#%3d M=%.3g gain=%.3g P=%.3g mult=%.3g"%r)
    return rows, inf, t


def make3(T0,k,p0,cg,cm,kper=0.0005,S=8.0,gamma=0.5,rho=4.0):
    P=Params(); P.S=S; P.gamma=gamma; P.mi_thr=T0; P.mi_k0=k; P.mi_kper=kper; P.rho=rho; P.bonus_uses_mi=False
    P.mmin=1.0/k
    def gain(P,T):
        x=T/P.mi_thr
        if x<1: return 0
        lg=math.log10(x)
        return math.floor(10**min(p0*lg-cg*lg*lg,300)+1e-9)
    def mult(P,M,smi):
        kk=P.mi_k0+P.mi_kper*smi
        v=kk*M
        if v>1:
            lg=math.log10(v)
            v=10**min(lg-cm*lg*lg,300)
        return 1+v
    P.gain=gain; P.mult=mult
    return P


def slopes(rows):
    pts=[(r[0],math.log10(max(r[5],1)),r[1]) for r in rows]
    def at(h):
        sel=[p for p in pts if p[0]<=h]
        return sel[-1] if sel else (0,0,0)
    out=[]
    for a,b in ((0,10),(10,25),(25,50),(50,100)):
        pa,pb=at(a),at(b)
        out.append("%.2f"%((pb[1]-pa[1])/(b-a)))
    return "slope(decades/h) "+"/".join(out)+"  cycle(min)@10,25,50,100h="+"/".join("%.0f"%at(h)[2] for h in (10,25,50,100))


if __name__=="__main__":
    S=float(sys.argv[1]) if len(sys.argv)>1 else 8.0
    H=float(sys.argv[2]) if len(sys.argv)>2 else 100.0
    # The shipped constants (KnowledgeRules.cs): T0, k, p0, cg, cm.
    P=make3(1e14,0.01,0.98,1e-4,5e-5,S=S,rho=4.0)
    rows,inf,t=run(P,hours=H,dt=15.0,verbose=False)
    print("base rate %g: first shift at %.2f h, %d shifts, log10(P) at end %.0f%s"%(S,rows[0][0] if rows else -1,len(rows),math.log10(max(rows[-1][5],1)) if rows else 0," (INF)" if inf else ""))
    print(slopes(rows))
