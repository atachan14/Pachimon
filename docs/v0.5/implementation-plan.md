# v0.5 Implementation Plan

## Phase 1: Recovery Domain

1. [完了] `RestSpotRecoveryService`を作る
2. [要更新] Effective MaxHP / MaxMN基準の割合回復を実装する
3. [完了] 端数切り上げと最大HP Clampを実装する
4. [完了] 復活数と回復量を結果として返す

## Phase 2: Node接続

1. [完了] Map生成時の回復率を50%へ変更する
2. [完了] RestSpotの`休む`操作を実装する
3. [要更新] HP / MN回復後にLeftPaneを更新する
4. [完了] 結果Logの確認後にNodeを完了する

## 完成確認

1. HPが減った3体をそれぞれ50%回復できる
2. CurrentHPが0の個体が復活する
3. Effective MaxHPが奇数なら回復量を切り上げる
4. Effective MaxHPを超えて回復しない
5. 全快状態でも進行できる
6. 結果確認前にNodeが完了しない
7. CurrentMNをEffective MaxMNの50%回復し、最大値を超えない
