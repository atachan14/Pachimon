# Architecture Notes

縺薙・繝輔ぃ繧､繝ｫ縺ｯ縲ゞnity螳溯｣・ｒ蟋九ａ繧句燕謠舌〒縺ｮ雋ｬ蜍吝・髮｢繝｡繝｢縺ｨ縺吶ｋ縲・
## 逶ｮ逧・- UI蜈郁｡後〒菴懊▲縺ｦ繧ゅΟ繧ｸ繝・け縺悟ｴｩ繧後↑縺・ｈ縺・↓縺吶ｋ
- battle / run / map / UI 縺ｮ雋ｬ蜍吝｢・阜繧貞・縺ｫ謠・∴繧・- 蠕後・繝ｪ繝輔ぃ繧ｯ繧ｿ繝ｪ繝ｳ繧ｰ繧ｳ繧ｹ繝医ｒ荳九￡繧・
## 蝓ｺ譛ｬ譁ｹ驥・- UI縺ｯ陦ｨ遉ｺ縺ｨ蜈･蜉帛女莉倥□縺代ｒ諡・ｽ薙☆繧・- 險育ｮ励ｄ騾ｲ陦悟宛蠕｡縺ｯ繝ｭ繧ｸ繝・け螻､縺ｧ陦後≧
- 繝槭せ繧ｿ繝ｼ繝・・繧ｿ縺ｨ螳溯｡梧凾繝・・繧ｿ繧貞・髮｢縺吶ｋ
- DefinitionTable 縺ｨ Logic 繧貞・髮｢縺吶ｋ
- 騾ｲ陦檎憾諷九・ `State` 縺ｨ縺励※謖√▽

## 繝ｬ繧､繝､繝ｼ蛻・牡
### MasterData
- 逕滓・蜑阪・螳夂ｾｩ繝・・繧ｿ
- 蝓ｺ譛ｬ縺ｯ `DefinitionTable` 縺ｨ縺励※菫晄戟縺吶ｋ

蟇ｾ雎｡萓・
- `PachimonDefinitionTable`
- `SkillDefinitionTable`
- `PassiveDefinitionTable`
- `ModDefinitionTable`
- `TrainerDefinitionTable`
- `GymLeaderDefinitionTable`

### Definition Logic
- 蛟句挨謖吝虚繧呈球蠖薙☆繧・C# 繧ｳ繝ｼ繝・- DefinitionTable 縺ｨ縺ｯ蛻・屬縺吶ｋ

蟇ｾ雎｡萓・
- `SkillLogic`
- `PassiveLogic`
- `SkillLogicRegistry`
- `PassiveLogicRegistry`

### RuntimeData
- run荳ｭ縺ｫ螟牙喧縺吶ｋ繝・・繧ｿ
- battle繧вeward縺ｧ譖ｴ譁ｰ縺輔ｌ繧・
蟇ｾ雎｡萓・
- `PachimonInstance`
- `BattleState`
- `RunState`
- `MapNodeState`
- `NodeSessionState`

### Runtime Logic
- 繝ｫ繝ｼ繝ｫ蜃ｦ逅・ｒ諡・ｽ薙☆繧・- UI縺九ｉ迢ｬ遶九＠縺ｦ蜍輔￠繧九％縺ｨ繧堤岼讓吶↓縺吶ｋ

蟇ｾ雎｡萓・
- `BattleController`
- `BattleResolver`
- `MapGenerator`
- `RewardResolver`
- `MapRunController`
- `RestSpotController`
- `CityController`

### UI
- 逕ｻ髱｢陦ｨ遉ｺ
- 繝懊ち繝ｳ蜈･蜉・- 蜷・憾諷九・隕九◆逶ｮ蛻・ｊ譖ｿ縺・
蟇ｾ雎｡萓・
- `HeaderView`
- `LeftPaneView`
- `MainPaneView`
- `RightPaneView`
- `MapOverlayView`
- `RewardOverlayView`

蟇ｾ雎｡萓・
- `StartScreen`
- `BattleScreen`
- `CityScreen`
- `RestSpotScreen`
- `LeagueGateScreen`

## 雋ｬ蜍吶・蛻・￠譁ｹ
### DefinitionTable縺後ｄ繧九％縺ｨ
- 蜈ｱ騾壹ョ繝ｼ繧ｿ菫晄戟
- 陦ｨ遉ｺ蜷阪ｄ繧ｳ繧ｹ繝医↑縺ｩ縺ｮ螳夂ｾｩ
- id繧帝壹§縺溷盾辣ｧ蜈・↓縺ｪ繧九％縺ｨ

### Definition Logic縺後ｄ繧九％縺ｨ
- skill / passive 縺ｮ蛟句挨謖吝虚
- trigger縺斐→縺ｮ蜃ｦ逅・- 迚ｹ谿頑擅莉ｶ蛻､螳・
### Registry縺後ｄ繧九％縺ｨ
- `id` 縺九ｉ蟇ｾ蠢懊☆繧・Logic 繧貞叙蠕励☆繧・- battle蛛ｴ繧・vent蛛ｴ縺・`id` 繝吶・繧ｹ縺ｧ蜃ｦ逅・ｒ蜻ｼ縺ｹ繧九ｈ縺・↓縺吶ｋ

### UI縺後ｄ繧九％縺ｨ
- state繧定｡ｨ遉ｺ縺吶ｋ
- 蜈･蜉帙う繝吶Φ繝医ｒ騾√ｋ
- 迴ｾ蝨ｨ逕ｻ髱｢繧貞・繧頑崛縺医ｋ

### UI縺後ｄ繧峨↑縺・％縺ｨ
- 繝繝｡繝ｼ繧ｸ險育ｮ・- tick騾ｲ陦・- reward蜿肴丐
- map逕滓・
- save蜀・ｮｹ縺ｮ豎ｺ螳・
### Logic縺後ｄ繧九％縺ｨ
- battle騾ｲ陦・- 陦悟虚蜿ｯ蜷ｦ蛻､螳・- 蜍晄風蛻､螳・- node隗｣豎ｺ
- reward遒ｺ螳・- run騾ｲ陦・- node逕ｻ髱｢縺斐→縺ｮController襍ｷ蜍・
## battle縺ｾ繧上ｊ縺ｮ雋ｬ蜍・### BattleState
- 迴ｾ蝨ｨ縺ｮbattle迥ｶ豕√ｒ菫晄戟縺吶ｋ

菫晄戟蛟呵｣・
- 蜻ｳ譁ｹ荳隕ｧ
- 謨ｵ荳隕ｧ
- 迴ｾ蝨ｨtick
- 迴ｾ蝨ｨ陦悟虚荳ｭunit
- 蜍晄風迥ｶ諷・- battle繝ｭ繧ｰ

### BattleController
- battle蜈ｨ菴薙・騾ｲ陦後ｒ邂｡逅・☆繧・
蠖ｹ蜑ｲ:
- 謌ｦ髣倬幕蟋・- 蜈･蜉帛女莉伜・縺ｮ蛻ｶ蠕｡
- 繧ｿ繝ｼ繝ｳ騾ｲ陦・- 蜍晄風遒ｺ螳・
### BattleResolver
- 1蝗槭・陦悟虚隗｣豎ｺ繧呈球蠖薙☆繧・
蠖ｹ蜑ｲ:
- skill菴ｿ逕ｨ
- 蟇ｾ雎｡隗｣豎ｺ
- 繝繝｡繝ｼ繧ｸ險育ｮ・- CD譖ｴ譁ｰ
- 迥ｶ諷区峩譁ｰ

### SkillLogicRegistry / PassiveLogicRegistry
- `id` 縺九ｉ蟇ｾ蠢懊☆繧・Logic 繧貞叙蠕励☆繧・- battle隗｣豎ｺ譎ゅ・蛻・ｲ舌ｒ髮・ｸｭ邂｡逅・☆繧・
## map縺ｾ繧上ｊ縺ｮ雋ｬ蜍・### RunMap
- 1run縺ｧ菴ｿ縺・ap蜈ｨ菴薙ｒ菫晄戟縺吶ｋ

### MapRow
- row縺斐→縺ｮnode鄒､繧剃ｿ晄戟縺吶ｋ

### MapNode
- 1縺､縺ｮnode諠・ｱ繧剃ｿ晄戟縺吶ｋ

菫晄戟蛟呵｣・
- nodeId
- rowIndex
- columnIndex
- nodeType
- 謗･邯壼・
- 隗｣豎ｺ蜀・ｮｹ

### MapGenerator
- map-generation.md 縺ｮ繝ｫ繝ｼ繝ｫ縺ｫ蠕薙▲縺ｦ map 繧堤函謌舌☆繧・
## run縺ｾ繧上ｊ縺ｮ雋ｬ蜍・### RunState
- 1run蜈ｨ菴薙・騾ｲ陦檎憾諷九ｒ菫晄戟縺吶ｋ

菫晄戟蛟呵｣・
- 謇謖√ヱ繝√Δ繝ｳ
- gold
- badge
- 迴ｾ蝨ｨ菴咲ｽｮ
- 迴ｾ蝨ｨnode
- 逕滓・貂医∩map

### MapRunController
- map / node 縺ｮ謗･邯壹ｒ諡・ｽ薙☆繧・- 蜷・ode逕ｨController縺ｮ襍ｷ蜍輔→騾ｲ陦檎ｮ｡逅・ｒ諡・ｽ薙☆繧・
蠖ｹ蜑ｲ:
- node驕ｸ謚・- 蟇ｾ雎｡node逕ｨController縺ｮ襍ｷ蜍・- Controller縺九ｉ騾ｲ陦悟庄閭ｽ縺ｮ菫｡蜿ｷ繧貞女縺大叙繧・- 谺｡node縺ｸ騾ｲ陦・- 谿ｿ蝣ょ・繧・/ 謨怜圏蜃ｦ逅・
### node逕ｨController
- node遞ｮ蛻･縺斐→縺ｮ逕ｻ髱｢縺ｨ騾ｲ陦悟宛蠕｡繧呈球蠖薙☆繧・
蟇ｾ雎｡萓・
- `BattleController`
- `RestSpotController`
- `CityController`
- `LeagueGateController`

### 騾ｲ陦悟庄閭ｽ迥ｶ諷・- node縺斐→縺ｫ縲梧ｬ｡Node縺ｸ騾ｲ繧√ｋ縺九阪ｒ菫晄戟縺吶ｋ
- battleNode縺ｯ蜍晏茜縺吶ｋ縺ｾ縺ｧ騾ｲ陦御ｸ榊庄
- battle邨ゆｺ・ｾ後↓騾ｲ陦悟庄閭ｽ縺ｸ蛻・ｊ譖ｿ縺医ｋ
- 繝代メ繝｢繝ｳ繧ｻ繝ｳ繧ｿ繝ｼNode縺ｯ蝗槫ｾｩ蠕後↓騾ｲ陦悟庄閭ｽ縺ｸ蛻・ｊ譖ｿ縺医ｋ
- 繧ｷ繝・ぅNode縺ｯ髢句ｧ区凾轤ｹ縺ｧ騾ｲ陦悟庄閭ｽ

## UI鬪ｨ邨・∩縺ｮ閠・∴譁ｹ
### Header
- 蝗ｺ螳壽ュ蝣ｱ陦ｨ遉ｺ

蛟呵｣・
- gold
- 迴ｾ蝨ｨ陦・- badge謨ｰ
- Map髢矩哩繝懊ち繝ｳ
- item繝代ロ繝ｫ髢矩哩繝懊ち繝ｳ
- 險ｭ螳壹・繧ｿ繝ｳ

陬懆ｶｳ:
- 讓ｪ髟ｷ逕ｻ髱｢縺ｧ縺ｯ螻樊ｧ縺斐→縺ｮbadge謨ｰ繧り｡ｨ遉ｺ縺吶ｋ
- 邵ｦ髟ｷ逕ｻ髱｢縺ｧ縺ｯ繧ｯ繝ｪ繝・け縺ｧ螻樊ｧ縺斐→縺ｮbadge謨ｰ繝昴ャ繝励い繝・・繧定｡ｨ遉ｺ縺吶ｋ

### LeftPane
- 荳ｻ縺ｫ蜻ｳ譁ｹ諠・ｱ陦ｨ遉ｺ

### MainPane
- 迴ｾ蝨ｨ逕ｻ髱｢縺ｮ繝｡繧､繝ｳ陦ｨ遉ｺ

蛟呵｣・
- battle
- start
- city
- restSpot
- leagueGate
- 謨怜圏貍泌・
- 谿ｿ蝣ょ・繧頑ｼ泌・

陬懆ｶｳ:
- map縺ｯMainPane繧偵⊇縺ｼ螳悟・縺ｫ隕・≧蛻･繧ｦ繧｣繝ｳ繝峨え縺ｨ縺励※陦ｨ遉ｺ縺吶ｋ
- reward縺ｯ `BattleScreen` 蜀・Κ縺ｧ縲｜attle逕ｻ髱｢繧貞濠蛻・ｻ･荳願ｦ・≧蛻･繧ｦ繧｣繝ｳ繝峨え縺ｨ縺励※陦ｨ遉ｺ縺吶ｋ

### RightPane
- 荳ｻ縺ｫ謨ｵ諠・ｱ繧・ode隧ｳ邏ｰ陦ｨ遉ｺ

## 螳溯｣・・譛溘・縺翫☆縺吶ａ
1. `HeaderView / LeftPaneView / MainPaneView / RightPaneView` 縺ｮ譫縺縺台ｽ懊ｋ
2. `BattleState` 縺ｨ `BattleController` 縺ｮ譛蟆冗沿繧剃ｽ懊ｋ
3. `BattleView` 繧・main pane 縺ｫ蟾ｮ縺苓ｾｼ繧
4. 莉ｮ縺ｮ蝗ｺ螳哺ap縺九ｉ battle 繧貞他縺ｶ

## 驕ｿ縺代◆縺・％縺ｨ
- UI繧ｹ繧ｯ繝ｪ繝励ヨ蜀・〒逶ｴ謗･繝繝｡繝ｼ繧ｸ險育ｮ励☆繧・- View縺軍untimeData繧呈嶌縺肴鋤縺医ｋ
- 逕ｻ髱｢縺斐→縺ｫ迢ｬ閾ｪ縺ｮ騾ｲ陦檎憾諷九ｒ謖√▲縺ｦ蛻・ｲ舌′蠅励∴繧・- MasterData繧堤峩謗･battle荳ｭ縺ｫ遐ｴ螢顔噪譖ｴ譁ｰ縺吶ｋ

## 蜻ｽ蜷阪・逶ｮ螳・- 螳夂ｾｩ繝・・繧ｿ: `XxxMaster`
- 螳溯｡梧凾繝・・繧ｿ: `XxxState` `XxxInstance`
- 蜃ｦ逅・球蠖・ `XxxController` `XxxResolver` `XxxGenerator`
- 繝ｬ繧､繧｢繧ｦ繝医ｄUI驛ｨ蜩・ `XxxView`
- MainPane 蜀・・蜷・node 逕ｻ髱｢: `XxxScreen`

## DefinitionTable 譁ｹ驥・### 縺翫☆縺吶ａ
- `SkillDefinitionTable` `PassiveDefinitionTable` `ModDefinitionTable` `TrainerDefinitionTable` `GymLeaderDefinitionTable` 縺ｯ螳夂ｾｩ繝・・繧ｿ縺ｨ縺励※謖√▽
- `PachimonDefinitionTable` 縺ｯ縲御ｸ隕ｧ邂｡逅・＠繧・☆縺・｡ｨ繝・・繧ｿ縲阪ｒ豁｣縺ｨ縺励ゞnity蛛ｴ縺ｧ縺ｯ隱ｭ縺ｿ霎ｼ縺ｿ逕ｨ繧｢繧ｻ繝・ヨ縺ｸ螟画鋤縺吶ｋ譁ｹ蠑上ｒ縺翫☆縺吶ａ縺吶ｋ

### 逅・罰
- 繝代メ繝｢繝ｳ謨ｰ縺・50蜑榊ｾ後↓縺ｪ繧九→縲・菴・SO縺ｮ謇句・蜉幃°逕ｨ縺ｯ驥阪￥縺ｪ繧翫ｄ縺吶＞
- 荳隕ｧ縺ｧ豈碑ｼ・∬､・｣ｽ縲∵､懃ｴ｢縲・㍾縺ｿ隱ｿ謨ｴ繧偵☆繧九↑繧芽｡ｨ蠖｢蠑上・縺ｻ縺・′蠑ｷ縺・- 蜿ら・髢｢菫ゅｄ逕ｻ蜒冗ｴ蝉ｻ倥￠縺ｯUnity蛛ｴ繧｢繧ｻ繝・ヨ縺ｮ縺ｻ縺・′謇ｱ縺・ｄ縺吶＞

### 螳溷漁逧・↑譯・1. `PachimonDefinitionTable` 縺ｮ蜈・ョ繝ｼ繧ｿ縺ｯ CSV / Excel / Google Sheets 縺ｪ縺ｩ陦ｨ縺ｧ邂｡逅・☆繧・2. Unity editor諡｡蠑ｵ繧・う繝ｳ繝昴・繧ｿ縺ｧ ScriptableObject 縺ｾ縺溘・蜀・ΚDB繧｢繧ｻ繝・ヨ縺ｸ螟画鋤縺吶ｋ
3. 逕ｻ蜒丞盾辣ｧ繧・・譛殱kill蜿ら・縺ｯ ID 繝吶・繧ｹ縺ｧ邨舌・縲∝､画鋤譎ゅ↓螳溷盾辣ｧ縺ｸ隗｣豎ｺ縺吶ｋ

### 蛻晄悄skill蜿ら・縺ｫ縺､縺・※
- ScriptableObject 蜷悟｣ｫ縺ｧ蜿ら・縺ｯ蜿ｯ閭ｽ
- 縺溘□縺・`PachimonDefinitionTable` 繧・譫售O縺ｫ蜈ｨ驛ｨ隧ｰ繧∬ｾｼ繧繧医ｊ縲∬｡ｨ縺九ｉ逕滓・縺吶ｋ縺ｻ縺・′邂｡逅・＠繧・☆縺・
## Skill / Passive 縺ｮ譁ｹ驥・### 縺翫☆縺吶ａ
- Skill 縺ｨ Passive 縺ｯ `DefinitionTable + Logic + Registry` 縺ｧ蛻・￠繧・
### 逅・罰
- 蜈ｱ騾夐・岼縺ｯ陦ｨ縺ｧ荳隕ｧ邂｡逅・＠縺溘＞
- 逋ｺ蜍墓擅莉ｶ繧・ｧ｣豎ｺ蜃ｦ逅・・蛟句挨繧ｳ繝ｼ繝峨′蠢・ｦ・- `id` 繧定ｻｸ縺ｫ縺吶ｋ縺ｨ battle蛛ｴ繧貞腰邏斐↓菫昴■繧・☆縺・
### 蠖ｹ蜑ｲ蛻・球
- `SkillDefinitionTable`: 蜷榊燕縲，D縲√さ繧ｹ繝医∬ｪｬ譏弱↑縺ｩ縺ｮ蜈ｱ騾夐・岼
- `SkillLogic`: 蛟句挨逋ｺ蜍募・逅・- `SkillLogicRegistry`: `skillId -> SkillLogic`
- `PassiveDefinitionTable`: 蜷榊燕縲》rigger縲∬ｪｬ譏弱↑縺ｩ縺ｮ蜈ｱ騾夐・岼
- `PassiveLogic`: 蛟句挨逋ｺ蜍募・逅・- `PassiveLogicRegistry`: `passiveId -> PassiveLogic`

## battle繝ｭ繧ｰ縺ｮ謖√■譁ｹ
### 縺翫☆縺吶ａ
- `BattleState` 縺ｫ陦ｨ遉ｺ逕ｨ縺ｮ霆ｽ縺・Ο繧ｰ驟榊・繧呈戟縺､
- 蜷・Ο繧ｰ縺ｯ1陦後ユ繧ｭ繧ｹ繝医〒縺ｯ縺ｪ縺上∫ｨｮ鬘樔ｻ倥″繧､繝吶Φ繝医→縺励※謖√▽

繝ｭ繧ｰ萓・
- `TurnStart`
- `SkillUsed`
- `DamageDealt`
- `UnitDown`
- `BattleEnd`

## ViewModel 螻､繧堤ｽｮ縺上°縺ｩ縺・°
### 縺翫☆縺吶ａ
- 譛蛻昴°繧臥峡遶九＠縺溷､ｧ縺阪＞ ViewModel 螻､縺ｯ鄂ｮ縺九↑縺上※繧医＞
- 縺溘□縺・UI陦ｨ遉ｺ逕ｨ縺ｮ謨ｴ蠖｢繝・・繧ｿ縺ｯ蟆上＆縺丞・髮｢縺励※繧医＞

### 譁ｹ驥・- 縺ｾ縺壹・ `State + Controller + View` 繧貞渕譛ｬ縺ｫ縺吶ｋ
- UI謨ｴ蠖｢縺碁㍾縺上↑縺｣縺溘→縺薙ｍ縺縺・`Presenter` 縺ｾ縺溘・霆ｽ縺・`ViewModel` 繧定ｶｳ縺・
## TODO
- Pachimon陦ｨ繝・・繧ｿ縺ｮ邂｡逅・ｪ剃ｽ薙ｒ豎ｺ繧√ｋ
- CSV繧､繝ｳ繝昴・繝医↓縺吶ｋ縺・Editor諡｡蠑ｵ縺ｫ縺吶ｋ縺・- battle繝ｭ繧ｰ縺ｮ陦ｨ遉ｺ邊貞ｺｦ
- Presenter 繧貞ｰ主・縺吶ｋ蠅・阜

## Compact / Expanded Layout Note (2026-04-05)
- UI 縺ｯ `Compact` 縺ｨ `Expanded` 縺ｮ 2 繝｢繝ｼ繝峨ｒ謖√▽蜑肴署縺ｧ騾ｲ繧√ｋ縲・- `GameRootView` 縺悟ｹ・ｒ隕九※ layout 繧貞・繧頑崛縺医ｋ縲・- `LayoutMode` 縺ｯ UI 雋ｬ蜍吶〒縺ゅｊ縲～MapRunController` 繧・`BattleController` 縺ｯ遏･繧峨↑縺上※繧医＞縲・- Controller / State 縺ｯ逕ｻ髱｢繧ｵ繧､繧ｺ縺ｫ萓晏ｭ倥＠縺ｪ縺・・


## Script Folder Structure (2026-04-06)
- `Assets/Scripts` は `Runtime / Editor` に分ける。
- UI は `Runtime/UI/Views`, `Runtime/UI/Installers`, `Runtime/UI/Bootstrap` を基本にする。
- battle / map / run / editor importer は別フォルダに分離する。
- 詳細は `docs/script-folder-structure.md` を参照する。
