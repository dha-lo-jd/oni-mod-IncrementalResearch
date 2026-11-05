"""Balance simulator for the idle game (best-play model). Not shipped with the mod.

Mirrors Core/KnowledgeRules.cs and Core/KnowledgeState.cs: greedy purchases, a permanent unlock of tiers 2 and 3
and a shift policy "shift when the gain reaches rho * max(Meta-Insight earned, 1/k)". Use it to retune the
constants. See docs/balance.md.

Run:  python tools/balance_sim.py [base_rate] [hours]
"""
import math
import os
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
 ("T4_FOCUS",4,5e16,"mul",2,5,None,"I",0,False),
 ("T5_RESONANCE",5,5e19,"mul",2,5,None,"I",0,False),
 ("T6_APERTURE",6,5e22,"mul",2,5,None,"I",0,False),
 ("T7_CHANNEL",7,5e25,"mul",2,5,None,"I",0,False),
 ("T4_AUTO_EXPAND",4,3e17,"autoexp",0,12,None,"I",0,True),
 ("T5_AUTO_EXPAND",5,3e20,"autoexp",0,12,None,"I",0,True),
 ("T6_AUTO_EXPAND",6,3e23,"autoexp",0,12,None,"I",0,True),
 ("T7_AUTO_EXPAND",7,3e26,"autoexp",0,12,None,"I",0,True),
 ("M_HEADSTART",0,3,"start",100,0,None,"M",1,True),
 ("M_DISCOUNT",1,8,"disc",0.5,0,None,"M",2,True),
]
# Singularity (docs/spec-singularity.md): needs tier 7, TOP_EXP expansions of tier 7 in the current cycle and every
# automatic expansion. Data Banks are assumed to be delivered. CC buys production x1.15 and MI gain x1.1 expansions.
TOP_EXP = 45
CC_PROD, CC_MI, CC_COST0, CC_COSTG = 1.15, 1.1, 2.0, float(os.environ.get('CCCG', '1.6'))
# Variant: the first expansions are stronger. Step n multiplies by 1 + a / (1 + n * d); a = 0 keeps the flat CC_PROD/CC_MI.
CC_A_PROD = float(os.environ.get('CCA_P', '0.7')); CC_A_MI = float(os.environ.get('CCA_M', '0.35')); CC_D = float(os.environ.get('CCD', '0.15'))
FIXP = float(os.environ.get('FIXP', '0')); FIXM = float(os.environ.get('FIXM', '0'))   # test: hold the CC multipliers fixed
CC_R = float(os.environ.get('CCR', '0.8'))   # > 0: step n multiplies by 1 + a * R^n, so the total converges
CC_GAIN_SCALE = float(os.environ.get('CCG', '5'))   # multiplies the Critical Constant gained per singularity
CC_COST_SCALE = float(os.environ.get('CCC', '1'))   # multiplies the cost of the Critical Constant expansions
def cc_mult(n, flat, a):
    if a <= 0: return flat ** n
    r = 1.0
    for i in range(n): r *= 1 + (a * CC_R ** i if CC_R > 0 else a / (1 + i * CC_D))
    return r
SING_LOG = []   # (hours, singularity number, shifts done in the run, CC gained)
AUTOS = ['T1_AUTO_EXPAND','T4_AUTO_EXPAND','T5_AUTO_EXPAND','T6_AUTO_EXPAND','T7_AUTO_EXPAND']
# Tiers 4-7 are draft values (docs/spec-tiers-4-7.md); NT = number of tiers simulated.
NT = 3
SAVE_FACTOR = float(__import__('os').environ.get('SAVE','0'))   # 0 = pure greedy
BASE = {1:10.0, 2:1000.0, 3:1e6, 4:1e11, 5:3e12, 6:1e14, 7:3e15}
GROW = {1:1.15, 2:1.18, 3:1.22, 4:1.25, 5:1.28, 6:1.31, 7:1.34}
UNLOCK = {2:25, 3:15, 4:30, 5:30, 6:30, 7:30}   # expansions of the tier below
BONUS = {2:0.002, 3:0.001, 4:0.0005, 5:0.00025, 6:0.0001, 7:0.00005}   # build-up rate of tier t on tier t-1

SHIFT_INFO = []   # (hours, shift number, expansions of tier 7 at the shift, all auto-expansions bought)
EVENTS = []   # (hours, name) of upgrade purchases, tier unlocks and shifts, for the event spacing report

SING = bool(os.environ.get('SING'))


def run(P, hours=100, dt=2.0, verbose=True, label=""):
    gain = P.gain or default_gain
    multf = P.mult or default_mult
    t = 0.0
    ex = {i:0 for i in range(1,NT+1)}
    ups = set(); mups = set()
    know = 0.0; T = 0.0
    Mearned = 0; Mspent = 0; smi = 0; shifts = 0
    unl = {i:(i==1) for i in range(1,NT+1)}
    b = {i:0.0 for i in range(1,NT+1)}
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
        return r * (FIXP if FIXP else cc_mult(ccp, CC_PROD, CC_A_PROD))
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
        if u[1]!=0 and not unl.get(u[1],False): return False
        return sc>=u[5] and shifts>=u[8] and (u[6] is None or has(u[6]))
    def start_know():
        return 100.0 if has("M_HEADSTART") else 0.0
    know = start_know()
    sings = 0; cc = 0; ccp = 0; ccm = 0; cc_spent = 0
    while t < end:
        Seff = P.Sref*(P.S/P.Sref)**P.gamma
        mi = multf(P, Mearned, smi)
        n1 = ex[1]+1
        mu1 = promul(1)*mi
        E1 = 1+b[1]
        prod = Seff*n1*mu1*E1
        if math.isinf(prod) or prod>1e300:
            inf=True; break
        know += prod*dt; T += prod*dt
        peakP = prod
        for tt in range(NT,1,-1):
            if unl[tt]:
                mt = promul(tt)*(mi if P.bonus_uses_mi else 1.0)
                Et = 1+b[tt] if tt<NT else 1.0
                b[tt-1] += dt*BONUS[tt]*Seff*(ex[tt]+1)*mt*Et
        # purchases (greedy cheapest first)
        while True:
            opts = []
            for u in U:
                if u[7]=="I" and vis(u) and u[2]<=know: opts.append((u[2],"u",u))
            for tier in range(1,NT+1):
                if not unl[tier]: continue
                c = cost(tier)
                if c<=know: opts.append((c,"e",tier))
            # A player saves up for a visible upgrade that is not far out of reach instead of buying expansions forever.
            saving = [u for u in U if u[7]=="I" and vis(u) and u[2]>know and u[2] <= SAVE_FACTOR*min([cost(tr) for tr in range(1,NT+1) if unl[tr]])]
            if saving:
                opts = [o for o in opts if o[1]=="u"]
            if not opts: break
            opts.sort(key=lambda o:o[0])
            c,k,x = opts[0]
            know -= c
            if k=="u": ups.add(x[0]); EVENTS.append((t/3600,'U:'+x[0]+('' if shifts==0 else '@s%d'%shifts)))
            else: ex[x]+=1
            for tt in range(2,NT+1):
                if not unl[tt] and ex[tt-1]>=UNLOCK[tt]: unl[tt]=True; EVENTS.append((t/3600,'TIER%d'%tt))
        if os.environ.get('TL') and shifts==0 and int(t)%300==0 and t>0 and int(t)!=getattr(P,'_lt',-1):
            P._lt=int(t); print('t=%5.1fmin T=%.2g know=%.2g P=%.2g ex=%s'%(t/60,T,know,prod,[ex[i] for i in range(1,NT+1)]))
        # shift decision
        g = int(gain(P, T) * (FIXM if FIXM else cc_mult(ccm, CC_MI, CC_A_MI)))
        cyc = t-cycle_start
        if g>=1 and g >= max(1, P.rho*max(Mearned, getattr(P,'mmin',0))):
            EVENTS.append((t/3600,'SHIFT%d'%(shifts+1)))
            rows.append((t/3600, cyc/60, shifts, Mearned, g, peakP, mi))
            SHIFT_INFO.append((t/3600, shifts+1, ex[7] if NT>=7 else 0, all(has(a) for a in AUTOS if next(x for x in U if x[0]==a)[1]<=NT)))
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
            ex = {i:0 for i in range(1,NT+1)}
            ups = set(u for u in ups if next(x for x in U if x[0]==u)[9])
            b = {i:0.0 for i in range(1,NT+1)}; T=0.0; know=start_know(); cycle_start=t
        if SING and NT>=7 and unl[7] and ex[7]>=TOP_EXP and all(has(a) for a in AUTOS):
            sings += 1
            gain_cc = max(1, min(10, int(math.floor(math.log10(max(Mearned, 1)))) - 5))   # KnowledgeRules.CcGain
            gain_cc = int(round(gain_cc * CC_GAIN_SCALE)); cc += gain_cc
            SING_LOG.append((t/3600, sings, shifts, gain_cc))
            EVENTS.append((t/3600,'SINGULARITY%d'%sings))
            # Spend CC on the cheaper expansion first, as the automation does.
            while True:
                cp = math.ceil(CC_COST0*CC_COST_SCALE*CC_COSTG**ccp); cm = math.ceil(CC_COST0*CC_COST_SCALE*CC_COSTG**ccm)
                c, kind = (cp, 0) if cp <= cm else (cm, 1)
                if c > cc - cc_spent: break
                cc_spent += c
                if kind == 0: ccp += 1
                else: ccm += 1
            ex = {i:0 for i in range(1,NT+1)}; ups=set(); mups=set()
            unl = {i:(i==1) for i in range(1,NT+1)}
            b = {i:0.0 for i in range(1,NT+1)}; T=0.0; know=start_know(); cycle_start=t
            Mearned = 0; Mspent = 0; smi = 0; shifts = 0
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


import os
for _t in range(4,8):
    BONUS[_t] *= float(os.environ.get('BONUS_SCALE','0.01'))
    BASE[_t] *= float(os.environ.get('BASE_SCALE','1'))

# Overrides for tuning the schedule of tiers 4-7:  BASES="b4,b5,b6,b7"  UNL="u4,u5,u6,u7"  MULC=100  AUTOC=1000
if os.environ.get('BASES'):
    for _t,_v in zip(range(4,8),os.environ['BASES'].split(',')): BASE[_t]=float(_v)
if os.environ.get('UNL'):
    for _t,_v in zip(range(4,8),os.environ['UNL'].split(',')): UNLOCK[_t]=int(_v)
_MULC=float(os.environ.get('MULC','100')); _AUTOC=float(os.environ.get('AUTOC','1000'))
_REQ=int(os.environ.get('REQMUL','5')); _REQA=int(os.environ.get('REQAUTO','12'))
U[:] = [(u[0],u[1],(BASE[u[1]]*(_MULC if u[0].endswith(('FOCUS','RESONANCE','APERTURE','CHANNEL')) else _AUTOC) if u[1]>=4 and u[7]=='I' else u[2]),u[3],u[4],
         ((_REQ if u[3]=='mul' else _REQA) if u[1]>=4 else u[5]),u[6],u[7],u[8],u[9]) for u in U]

if __name__=="__main__":
    S=float(sys.argv[1]) if len(sys.argv)>1 else 8.0
    H=float(sys.argv[2]) if len(sys.argv)>2 else 100.0
    NT=int(sys.argv[3]) if len(sys.argv)>3 else 3
    # The shipped constants (KnowledgeRules.cs): T0, k, p0, cg, cm.
    P=make3(1e14,0.01,0.98,1e-4,5e-5,S=S,rho=4.0)
    rows,inf,t=run(P,hours=H,dt=float(os.environ.get("DT","15")),verbose=False)
    print("base rate %g: first shift at %.2f h, %d shifts, log10(P) at end %.0f%s"%(S,rows[0][0] if rows else -1,len(rows),math.log10(max(rows[-1][5],1)) if rows else 0," (INF)" if inf else ""))
    print(slopes(rows))
    if SING_LOG:
        prev=0
        for h,n,sh,g in SING_LOG:
            print('singularity %d at %.2fh (+%.1fh), %d shifts in that run, +%d CC'%(n,h,h-prev,sh,g)); prev=h
    if os.environ.get('EVENTS'):
        prev=0
        for h,n in EVENTS[:int(os.environ.get('EVENTS'))]:
            print('%7.3fh  +%6.1fmin  %s'%(h,(h-prev)*60,n)); prev=h
    if os.environ.get('FIRST'):
        seen={}
        for h,n in EVENTS:
            k=n.split('@')[0]
            if k.startswith('U:') and '@' in n: continue
            seen.setdefault(k,h)
        prev=0; mx=0
        for k,h in sorted(seen.items(),key=lambda kv:kv[1]):
            print('%7.3fh  +%6.1fmin  %s'%(h,(h-prev)*60,k)); mx=max(mx,h-prev); prev=h
        print('max gap %.1f min'%(mx*60))
