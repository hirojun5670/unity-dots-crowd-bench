# unity-dots-crowd-bench

Unity DOTS/ECS で大量エンティティ処理を検証するプロジェクトです。
Boid 理論による移動制御、索敵アルゴリズム（総当たり / 空間ハッシュ）の性能比較、
さらに ECS（データ指向）と MVP（VContainer / R3）で Entity の状態をモニタリングする設計を扱っています。

## デモ
![デモGIF](./docs/unity-dots-crowd-bench_trimmed_480p.gif)

### スクリーンショット
![スクリーンショット1](./docs/unity-dots-crowd-bench_ss.jpg)
![スクリーンショット2](./docs/unity-dots-crowd-bench%202026_07_23%2016_09_47.jpg)

## 概要
- 2陣営のユニットが出現し、Boid 理論（分離・整列・結合）で群れを形成しながら相手陣営と交戦します。
- FPS と処理時間をリアルタイムで計測・表示します。

## 技術スタック
- Unity 6.3 LTS / URP
- Unity Entities（DOTS/ECS）
- VContainer + R3（MVP, UI 層）
- URP / Entities Graphics

## アーキテクチャ
### System構成
Spawner → Targeting（BruteForce / SpatialHash）→ Boid → Damage → Despawner

| System名 | 役割 |
|---|---|
| Spawner | Entity のスポーンを行う |
| Targeting | 自陣営以外の Entity を総当たりで索敵する |
| SpatialHashTargeting | 自陣営以外の Entity を空間ハッシュで索敵する |
| Boid | Boid 理論で群れの動きを制御する |
| Damage | 索敵で発見した Entity にダメージを与える |
| Despawner | HP が 0 以下の Entity を削除する |

### ECSとMVPの境界設計
ECS から情報を取得する Model、情報を表示する UIView（UI Toolkit）、それらをつなぐ Presenter で MVP を構成しています。
Model は Entity の情報収集に専念し、UIView は受け取った情報の表示のみを担当します。
依存関係は VContainer で注入しています。

## パフォーマンス検証（2026/07/23）
### 検証環境
- 1000体の Entity
- ユニットサイズ: 1f x 1f
- cellSize = 2f
- 空間ハッシュ索敵の CellSpan = 1（自身のセル + 周囲 26 セルの計 27 セル）
- Job による並列化なし

### 検証結果
| System | Update にかかった時間 | FPS |
|---|---|---|
| 総当たり | 3.2ms | 140fps |
| 空間ハッシュ | 0.84ms | 190fps |
| Boid | 1.16ms | - |

※ Development Build で計測。

空間ハッシュ導入による高速化が確認できました。
Boid は索敵処理に直接関与しないため、別軸の一定負荷として存在します。

### 最適化の過程
1. sqrt の回数を抑えるよう計算式を見直し。
2. ComponentLookup の重複参照を削減し、不要なアクセスを抑制。
3. 空間ハッシュ索敵時に CellSpan が 2 以上になると探索セル数が急増するため、今回の検証では CellSpan = 1 を維持するよう調整。
4. Entity が密集しすぎると計算負荷が高まるため、分離（Separation）の Weight を高めに調整。
5. さらなる改善には Job による並列化が必要。

## 今後の展望
- JobEntity による並列化 [#21](https://github.com/hirojun5670/unity-dots-crowd-bench/issues/21)
- 陣営ごとのユニット総数を UI に表示 [#25](https://github.com/hirojun5670/unity-dots-crowd-bench/issues/25)

## ライセンス
The source code in this repository is licensed under the [MIT License](./LICENSE).