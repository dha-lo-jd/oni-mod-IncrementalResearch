# CLAUDE.md

ONI mod（研究ツリーの上の放置ゲーム「Project Ⅲ」）。共通事項は HQ（親フォルダ）の `CLAUDE.md`、仕様は `docs/`（`spec-*.md`、`concept.md`、`balance.md`）。Build: `dotnet build IncrementalResearch.sln`（Debug は `Local.Build.props` の `ModInstallPath` へ配備。コンパイル確認だけなら `-p:ModInstallPath=`。Release は `release/IncrementalResearch`）。

## 実機検証
`IncrementalResearch/HarnessTests.cs`（`#if DEBUG`、タグ `incrementalresearch`、12 件）: 本編向け報酬（費用の曲線、属性ボーナスの購入・上限・リセット・後から来た複製人間への付与、無料券、興味関心の付与とリセット、節目報酬と通知）、臨界定数（拡張の費用と倍率）、シンギュラリティ（データバンクの必要数と CC の獲得量、発動条件、リセットと残るもの、記録保管庫のデータバンク要求）、自動パラダイムシフトのしきい値。
実行: `GRAVITAS_EXCLUSIVE=1 python C:/Users/dha_1/ONI-Modding/GravitasTestHarness/Gravitas.TestProbe/probe.py run --project . --summary --tests '{"tags":["incrementalresearch"]}'`
ゲームを起動し本番の mods.json を書き換えるので、実行前にユーザーに確認する。`GRAVITAS_EXCLUSIVE=1`（この Mod だけを有効にする）を付けないと、他の自作 Mod のチュートリアルが画面を覆う。
テストは `KnowledgeState` / `RewardState` の private な状態をリフレクションで書き換える（`State`。数時間の進行で届く状態を作るため）。終わると元の値を戻す。フィールド名を変えたらテストも直す。
テストが見ていないもの: 知見タブなど UI の表示と操作、チュートリアルとシンギュラリティの確認ダイアログ、セーブ・ロードでの保存、実際の周回の速さ（`docs/spec-singularity.md` の「確認すること」のうち数値の調整）、複製人間によるデータバンクの運搬。
`csproj` に Debug 専用のハーネス参照（`HarnessDllPath`、`Local.Build.props` に書く）がある。Release には入らない。
