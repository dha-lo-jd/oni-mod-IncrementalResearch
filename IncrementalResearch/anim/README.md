# 設備のアニメーション置き場

設備ごとに、独自のアニメーションを置くフォルダです。ビルド時に、MODの出力先の `anim` フォルダへそのままコピーされます(`IncrementalResearch.csproj` の `PostBuild`)。

## フォルダの構成

`anim/assets/<名前>/` に、次の3ファイルを置きます。ファイル名の `<名前>` は、フォルダ名と同じにします。

| ファイル | 中身 |
|---|---|
| `<名前>.png` | スプライトシート |
| `<名前>_build.bytes` | ビルドデータ(スプライトの切り出し) |
| `<名前>_anim.bytes` | アニメーションデータ |

ゲームは、このMODのアニメーションを `<名前>_kanim` という名前で読み込みます。設備の定義(`BuildingTemplates.CreateBuildingDef` の kanim 引数)では、この名前を渡します。

## 設備とフォルダの対応

| 設備 | ID | フォルダ | いま流用しているアニメーション |
|---|---|---|---|
| 記録保管庫 | `IR_RecordArchive` | `IRRecordArchive` | 独自(`IRRecordArchive_kanim`)に差し替え済み |
| 計算室 | `IR_ComputeRoom` | `IRComputeRoom` | 独自(`IRComputeRoom_kanim`)に差し替え済み |
| 真空研究室 | `IR_VacuumLab` | `IRVacuumLab` | 独自(`IRVacuumLab_kanim`)に差し替え済み |
| 高エネルギー粒子加速器 | `IR_Accelerator` | `IRAccelerator` | 独自(`IRAccelerator_kanim`)に差し替え済み |
| 励起量子閉じ込め構造体 | `IR_Confinement` | `IRConfinement` | 独自(`IRConfinement_kanim`)に差し替え済み |
| コヒーレンス観測ネット | `IR_ObservationNet` | `IRObservationNet` | 独自(`IRObservationNet_kanim`)に差し替え済み |
| 知見縮退抽出炉 | `IR_ExtractionFurnace` | `IRExtractionFurnace` | 独自(`IRExtractionFurnace_kanim`)に差し替え済み |

## 差し替えの手順

1. 該当フォルダに3ファイルを置く。
2. `Buildings/` の設備定義で、kanim の名前を `<名前>_kanim` に変える。
3. ビルドして、ゲーム内で表示を確認する。

フォルダが空(`.gitkeep` だけ)の間は、今までどおり既存のアニメーションを使います。
