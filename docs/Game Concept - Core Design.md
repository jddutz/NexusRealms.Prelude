# Game Concept - Overview & Index

## Vision

A tactical strategy game with extensive replayability focused on:
- **Deterministic systems** with emergent complexity (skill > luck)
- **Synergy-based gameplay** (abilities, equipment, tactics combine to overcome challenges)
- **Predictable, fair difficulty** (no artificial HP/damage scaling, no ability removal)
- **Knowledge-based progression** (player mastery, not RNG rewards)
- **Solo developer friendly** (manageable scope, asset reuse)

### Target Experience
- **40 hours minimum** for meaningful playthrough
- **200+ hours potential** for deeply engaged players
- **Unlocking all characters = completionist goal** (not required for base experience)
- Focus on handcrafted content that rewards exploration and experimentation

## Documentation Index

### Narrative & Setting
- **[Storyline Setting](Storyline%20Setting.md)** - The world of NexusRealms, the Cradle, the Nexus, meta-narrative themes
- **[Portal System](Portal%20System.md)** - Inner Realms, gate structure, portal mechanics, world connections

### Core Systems
- **[Story Structure & Branching](Story%20Structure.md)** - Chapter system, moral decisions, branching paths, between-battle flow
- **[Character System](Character%20System.md)** - Character types, skill progression, unlocking, faction distribution
- **[Combat System](Combat%20System.md)** - Battle mechanics, difficulty design, synergies (mostly undefined)
- **[Progression & Equipment](Progression%20&%20Equipment.md)** - Time investment, shops, crafting, meta-progression
- **[Factions](Factions.md)** - Six faction system, themes, relationships (needs development)
- **[Infinite Mode](Infinite%20Mode.md)** - Post-game endgame content (to be designed)

### Faction Details
- **[Faction Template](Factions/__FACTION_TEMPLATE.md)** - Template for creating faction documents
- **[Order Faction](Factions/Order.md)** - Lawful Good divine warriors (example/in progress)
- **[Arcane Faction](Factions/Arcane.md)** - Chaotic Good spellcasters (to be created)
- **[Technology Faction](Factions/Technology.md)** - Lawful Neutral artificers (to be created)
- **[Nature Faction](Factions/Nature.md)** - Chaotic Neutral druids (to be created)
- **[Shadow Faction](Factions/Shadow.md)** - Lawful Evil rogues (to be created)
- **[Chaos Faction](Factions/Chaos.md)** - Chaotic Evil barbarians (to be created)

## High-Level Summary

### Core Loop
1. **Story Mode**: Play through 5-chapter story (45 battles, ~7.5 hours)
2. **Character Unlocking**: Defeat Final Boss to unlock party members
3. **New Game+**: Replay with different choices to unlock more characters
4. **Infinite Mode**: Post-game content for optimization and mastery

### Key Numbers
- **5 Chapters** to complete story
- **3 Runs per Chapter** (3 battles each)
- **45 battles total** per playthrough
- **6 characters** in party at completion (main + 5 companions)
- **5 skills** unlocked per character through story
- **6 factions** with 5+ characters each = **30+ total characters**
- **10 minutes** per battle target
- **Minimum 4 playthroughs** for core completion (~30 hours)

### Party Building Through Story
- Player builds squad/party over the course of story
- Synergies between party members drive tactical depth
- Story choices determine which characters join (some mutually exclusive)
- Different story paths = different party compositions = high replayability
- Choose starting character from 6 factions

### Two-Phase Progression

**Story Mode:**
- Play through main storyline
- Unlock characters through story decisions
- Characters gain skills at end of each chapter
- Defeat Final Boss to complete story
- Equipment goes to central inventory

**Infinite Mode (Post-Game):**
- Continue with unlocked characters from all playthroughs
- Further skill progression
- Equipment crafting and enhancement
- Increasingly difficult challenges
- Hundreds of hours of content

## Design Principles

### What Makes This Game Unique

**Deterministic Replayability:**
- No RNG in character acquisition (story-based unlocking)
- No random card draws or loot boxes
- Predictable systems allow strategic planning
- Replayability from story choices, not random chance

**Strategic Depth:**
- Synergy-based combat (abilities + equipment + tactics)
- Build crafting through party composition
- Competing priorities (characters vs equipment vs quests)
- Knowledge-based mastery

**Fair Challenge:**
- Difficulty through smart AI and tactics, not stat inflation
- No artificial scaling (HP bloat, damage scaling)
- No random ability removal or RNG punishments
- Player skill and strategy determine success

**Completionist-Friendly:**
- 40 hours feels complete
- 200+ hours for those who want it
- Clear goals and progression
- Unlocking all content is optional but achievable

---

## Detailed Design Documents

See the individual documents linked above for comprehensive design details, open questions, and development notes.

## Quick Reference

### Story Structure
**See [Story Structure.md](Story%20Structure.md) for full details.**

**Summary:**
- 5 chapters, 3 runs per chapter, 3 battles per run = 45 battles per playthrough
- ~7.5 hours per playthrough
- One companion joins per chapter
- Major moral decision at end of each chapter
- All characters gain skill at end of each chapter
- Choose starting character from 6 factions

### Characters & Skills
**See [Character System.md](Character%20System.md) for full details.**

**Summary:**
- 6 factions with 5+ characters each = 30+ total
- 6 characters per playthrough (main + 5 companions)
- 5 skills per character (unlock through story)
- Skills level up through use
- Unlock characters by defeating Final Boss with them
- Minimum 4 playthroughs for core content

### Combat
**See [Combat System.md](Combat%20System.md) for full details.**

**Summary:**
- **Target: 10 minutes per battle**
- Deterministic (no RNG mechanics)
- Synergy-based gameplay
- Party vs enemies
- **[UNDEFINED]**: Turn-based? Grid-based? Party size?

### Progression & Equipment
**See [Progression & Equipment.md](Progression%20&%20Equipment.md) for full details.**

**Summary:**
- Shops strategically placed (deterministic inventory)
- Quest rewards for items
- Can't get everything in one run
- Equipment goes to central inventory on completion
- Crafting system (post-story?)
- Strategic tension between characters/quests/shops

### Factions
**See [Factions.md](Factions.md) for full details.**

**Summary:**
- 6 factions with distinct identities
- **[UNDEFINED]**: Faction themes and playstyles
- Faction relationships drive story
- Moral decisions affect faction allegiances
- Starting character (one per faction)

### Infinite Mode
**See [Infinite Mode.md](Infinite%20Mode.md) for full details.**

**Summary:**
- Unlocked after defeating Final Boss
- Post-game endgame content
- 200+ hours of potential content
- **[UNDEFINED]**: Structure and gameplay loop

---

## Legacy Content Below (To Be Cleaned Up)

The sections below contain the original detailed content. This will be removed once all content is properly moved to the topic-specific documents.

### Five Chapter Structure (MOVED TO Story Structure.md)

**Story Progression:**
- **5 Chapters** to complete the main storyline
- **3 Runs per Chapter** (a "run" is a sequence of battles)
- Each chapter concludes with:
  - A new character joins the party
  - A moral decision that cuts off certain characters and future storyline options
  - All characters gain a new skill

### Character Progression Through Story
**Chapter 1:**
- Start with main character
- 3 runs of battles
- End: 1st companion joins, moral decision, all gain skill #1

**Chapter 2:**
- Party of 2 (main + 1 companion)
- 3 runs of battles
- End: 2nd companion joins, moral decision, all gain skill #2

**Chapter 3:**
- Party of 3
- 3 runs of battles
- End: 3rd companion joins, moral decision, all gain skill #3

**Chapter 4:**
- Party of 4
- 3 runs of battles
- End: 4th companion joins, moral decision, all gain skill #4

**Chapter 5:**
- Party of 5 (main + 4 companions)
- 3 runs of battles
- End: 5th companion joins, Final Boss, all gain skill #5, unlock Infinite Mode

**Total First Playthrough:**
- 15 runs (5 chapters × 3 runs)
- 45 battles total
- **6 characters in party** (main + 5 companions)
- 5 skills unlocked per character
- Moral decisions at end of each chapter shape available paths
- Dialog decisions between battles create additional branching
- Quest and shop choices affect available equipment and rewards

**Starting Character System:**
- **Choose starting character from each of 6 factions**
- Creates 6 different entry points into the story
- Story traversal differs based on starting choice
- Different cadence for each playthrough
- Reduces repetitive/linear feeling

**Multiple Completions:**
- Completing main storyline with same character multiple times
- Characters have **predetermined abilities at end of storyline** (5 skills)
- **Equipment goes to central inventory** upon completion
- Player can re-equip characters later from central inventory
- Allows optimization in New Game+ and Infinite Mode

### The Math - Time Investment - CONFIRMED

**Battle Structure:**
- **3 battles per run**
- **3 runs per chapter**
- **5 chapters** to complete main story
- **Total: 45 battles** for one complete playthrough

**Time Calculation:**
- 10 minutes per battle (target)
- 3 battles per run = 30 minutes per run
- 3 runs per chapter = 90 minutes (1.5 hours) per chapter
- **5 chapters = 450 minutes = 7.5 hours per playthrough**

**To Reach 40+ Hour Experience:**
- 6 characters in party at end (main + 5 companions)
- **Minimum 4 playthroughs** to unlock core character set
- 4 playthroughs × 7.5 hours = **30 hours** for core completion
- Additional playthroughs for completionist unlocks = **100+ hours potential**
- Infinite Mode adds hundreds more hours

### Replayability & Character Unlocking

**Faction-Based Character System:**
- **6 factions** in the game world
- **At least 5 characters per faction** = 30+ total characters
- Each faction has distinct themes, abilities, and synergies
- Faction allegiances affected by moral decisions

**Completionist Path:**
- Moral decisions create mutually exclusive paths
- Different playthroughs reveal different faction characters
- Some characters considerably harder to unlock (rare story paths)
- Unlocking all characters could take 100+ hours
- Natural replayability through faction variety (not artificial difficulty scaling)

**Character Unlock System:**
- Characters in your party when defeating Final Boss are unlocked
- 6 characters per playthrough (main + 5 companions)
- **Minimum 4 playthroughs** to unlock enough characters for full party choice
- But with 30+ characters, true completion requires many more runs
- New Game+ allows selecting from any unlocked characters
- Different moral decisions = different faction characters available
- Some characters may require very specific story paths

**Path Variety:**
- 5 moral decisions per playthrough (one per chapter)
- Each decision has 2+ options
- Creates 2^5 = 32 possible path combinations (minimum)
- Can design specific characters to require rare path combinations
- Faction allegiances can require multi-playthrough setup (e.g., must help Faction A in run 1 to unlock Faction A path in run 2)

## Moral Decisions & Story Branching

### Chapter-End Moral Decisions
- **One major moral decision at the end of each chapter**
- Choices are permanent - no going back
- Each decision cuts off certain characters and future storyline options
- Creates unique story paths with different character availability

### Impact of Decisions
- Determines which characters can join in future chapters
- May affect which trainers/skills become available for main character
- Some decisions create mutually exclusive character combinations
- Prevents overpowered party compositions
- Rewards strategic planning across playthroughs

### Story Network Design
- Not open world - every decision carries weight
- Choosing option A means option B is gone permanently
- Story is a network of branching nodes
- Some branches may converge at certain points
- Different paths lead to different characters
- Branching occurs at multiple levels:
  - Between battles (dialog choices)
  - End of chapters (major moral decisions)
  - Starting character selection (different entry points)

### Strategic Replayability
- Can't see all content in one playthrough
- Different moral choices = different characters
- Completionists must explore all branches
- 5 binary decisions = 32 minimum unique paths
- Can design more complex decision trees (3+ options per decision)
- Story doesn't feel repetitive or linear across playthroughs

## Critical Design Question: Story Flexibility

**The Challenge:**
- 3 battles per run × 3 runs per chapter × 5 chapters = **45 battle nodes per playthrough**
- Need to recruit 5 companions per playthrough (1 per chapter)
- 30+ total characters to unlock across all playthroughs
- 6 different starting points (faction starting characters)

**Is this structure sufficient for variety?**

**Potential Solutions:**

**Option A: Shared Battle Pool**
- Many battles accessible from multiple story paths
- Same battle content, different narrative context
- Recruitment opportunities vary by path taken
- 45 battles per playthrough from pool of 60-80 unique battles

**Option B: Dense Branching Within Runs**
- Each run has 2-3 optional battle branches
- Player chooses which battles to pursue
- Not all content seen in one playthrough
- Still maintain ~3 battles per run pacing

**Option C: Chapter-Specific Design**
- Early chapters (1-2): More linear (tutorial, introduce mechanics)
- Later chapters (3-5): Heavy branching for character recruitment
- Front-loaded teaching, back-loaded variety

**Option D: Starting Point Divergence**
- Each of 6 starting characters has unique path elements
- Paths converge at key story moments
- Ensures meaningful differences between playthroughs

**Likely Solution: Combination of Above**
- Use branching between battles (dialog choices)
- Chapter-end moral decisions gate major paths
- Starting character determines initial trajectory
- Some battles reused with different context
- Result: Players experience 45 battles per playthrough from pool of 75-100 unique battles

## Character & Skill System

### Main Character
- No fixed class
- Learns skills through story progression (trainers, events)
- Skill set determined by story path taken
- Total skill pool: **[TO BE DETERMINED]**
- Skills learned per playthrough: **[TO BE DETERMINED]**

### Companion Characters
- Each has fixed set of skills
- **5 skills per character** (unlocked through story chapters)
- **Skill unlock timing:** End of each chapter
  - Skill 1: End of Chapter 1
  - Skill 2: End of Chapter 2
  - Skill 3: End of Chapter 3
  - Skill 4: End of Chapter 4
  - Skill 5: End of Chapter 5 (Final Boss)
- All party members gain new skills simultaneously
- Skill progression tied to story pacing

### Skill Leveling
- After each battle, characters can improve skills they used
- Encourages trying different abilities
- Continues into Infinite Mode
- **[TO BE DETERMINED]**: Can you also train unused skills (slower)?

## Combat System

### Battle Structure
- **5-10 minute battles** (target)
- Player's party vs enemies
- **[TO BE DETERMINED]**: All 5 party members fight simultaneously?
- **[TO BE DETERMINED]**: Can swap characters between battles?
- **[TO BE DETERMINED]**: Turn-based? Real-time with pause? Action points?

### Battle Outcomes
- Victory: Progress story, gain rewards, improve skills
- Retreat: Can restart that battle or give up
- Main character death: Must restart that battle
- Losses are permanent (no save scumming)

### NPC Support
- Some battles include NPC allies
- **[TO BE DETERMINED]**: How does this work mechanically?

## Infinite Mode

### Post-Story Content
- Unlocked after defeating Final Boss
- Continue with unlocked characters
- Further progression opportunities:
  - Skill leveling continues
  - Equipment improvements
  - Crafting advanced enhancements
  - Unlock more difficult challenges

### Infinite Mode Structure
**[TO BE DETERMINED]**: What does a "run" look like?
- Option A: Chain of increasingly difficult battles (roguelike-style)
- Option B: Selectable missions/challenges with modifiers
- Option C: Procedural campaign with resource management

### Equipment & Crafting
**[TO BE DETERMINED]**: How to avoid grindy RNG?
- Deterministic crafting recipes?
- Materials drop predictably from specific challenge tiers?
- Equipment as build enablers (not just stat sticks)?

## Factions

**Six Factions (5+ characters each = 30+ total):**
- **[TO BE DETERMINED]**: Faction identities and themes
- Each faction has distinct abilities, equipment, and playstyles
- Faction synergies reward thematic team composition
- Moral decisions affect faction relationships

**Faction Design Goals:**
- Clear thematic identity (visually and mechanically)
- Unique synergies within faction
- Cross-faction combos for advanced strategies
- No single "best" faction - situational strengths
- Each faction has at least one starting character option

**Starting Character System:**
- 6 factions = 6 starting character options
- **[TO BE DETERMINED]**: Is starting character the "main character" or first companion?
- Affects story entry point and available paths
- Different starting points reduce repetition across playthroughs

## Critical Design Question: Story Flexibility

**The Challenge:**
- 3 battles per run × 3 runs per chapter × 5 chapters = **45 battle nodes**
- Need to recruit 5 companions per playthrough (1 per chapter)
- 30+ total characters to unlock across all playthroughs
- 6 different starting points (faction starting characters)

**Is 45 battle nodes enough flexibility?**

**Potential Approaches:**

**Option A: Shared Battle Pool**
- Many battles can be reached from multiple story paths
- Same battle content, different context
- Recruitment opportunities vary by path taken
- 45 unique battles, but 100+ total paths through them

**Option B: Dense Branching**
- Some runs have 2-3 battle branches (choose which to do)
- Not all content seen in one playthrough
- Actual battle count is higher (60-75 unique battles?)
- 45 battles per playthrough from larger pool

**Option C: Chapter-Specific Branches**
- Early chapters (1-2): More linear, introduce mechanics
- Later chapters (3-5): Heavy branching, character recruitment
- Chapters 1-2: 15-20 unique battles total
- Chapters 3-5: 40-50 unique battles (only see ~25 per playthrough)

**Option D: Starting Point Determines Path**
- Each of 6 starting characters has somewhat unique path through story
- Some overlap/convergence points
- ~60-80 unique battles total, see ~45 per playthrough
- Ensures different playthroughs feel meaningfully different

## Open Questions & Concepts to Consider

### Combat System Design
1. **Combat format**: Turn-based tactics? Real-time with pause? Action points?
2. **Party composition**: All 6 fight simultaneously? Rotating roster? Frontline/backline?
3. **Positioning mechanics**: Grid-based? Free movement? Terrain effects?
4. **Win conditions**: Eliminate all enemies? Objectives? Time limits?
5. **Pacing within 3-battle run**: How to maintain variety across consecutive battles?

### Difficulty & Challenge Scaling
1. **Initial difficulty curve**: How does Chapter 1 (solo) compare to Chapter 5 (party of 6)?
2. **New Game+ difficulty**: Harder enemies without artificial HP/damage scaling?
3. **Optional challenges**: Hard mode? Special conditions? Time trials?
4. **Replay with strong characters**: How to keep it challenging when player has unlocked powerful combos?
5. **Skill-based difficulty**: What makes a battle "hard" besides numbers?

### Progression Systems
1. **Skill leveling details**: How much do skills improve? Linear or diminishing returns?
2. **Post-battle skill selection**: Choose from skills used, or all available skills?
3. **Main character skill pool**: How many total skills can main learn across all playthroughs?
4. **Equipment system**: When do you get gear? How does it progress? Faction-specific?
5. **Meta-progression**: What carries over between playthroughs besides unlocked characters?

### Resource Management & Equipment
1. **Economy**: Is there currency? What do you buy?
2. **Between runs**: Any persistent resources or upgrades?
3. **Crafting system**: Introduced post-story? Deterministic recipes?
4. **Equipment management**: 
   - Equipment goes to central inventory upon storyline completion
   - Can re-equip characters from central pool
   - Faction restrictions on equipment?
5. **Healing/recovery**: How does party recover between battles?
6. **Quest system**: How are quests presented? Can you track which you've completed?

### Story & World Building
1. **Faction relationships**: Alliances? Conflicts? Neutral parties?
2. **Moral decision nature**: Good vs evil? Pragmatic choices? Faction allegiances?
3. **Story cohesion**: How do story branches maintain coherent narrative across 6 starting points?
4. **Character identity system**: 
   - Is "main character" always the same person, or does starting character choice change it?
   - If starting character is first companion, who is the actual main character?
5. **World scope**: Single region? Multiple kingdoms? Contained or expansive?
6. **Story network complexity**:
   - 45 battle nodes total
   - Dialog branches between battles
   - 5 chapter-end moral decisions
   - 6 different starting points
   - Is this enough flexibility for 30+ characters?

### Party Composition Rules
1. **Faction balance**: Can you have 6 characters from same faction? Restrictions?
2. **Character swapping**: Can you bench characters between battles? Between runs?
3. **Synergy design**: Should game encourage mixed-faction or mono-faction parties?
4. **Role requirements**: Do you need tank/healer/DPS, or flexible composition?

### Tutorial & Onboarding
1. **Chapter 1 teaching**: How to introduce all mechanics with solo main character?
2. **Complexity scaling**: When to introduce advanced mechanics (crafting, synergies, etc.)?
3. **First-time player experience**: How much guidance vs discovery?
4. **New Game+ differences**: Can skip tutorial? Faster progression?

### Infinite Mode Design
1. **Structure**: Endless battles? Selectable challenges? Campaign mode?
2. **Progression**: How do characters continue to grow?
3. **Rewards**: What keeps players engaged after unlocking all characters?
4. **Challenge variety**: How to maintain interest across hundreds of hours?

---

## Next Steps

- [ ] Clarify "run" definition (full story vs chapter/segment)
- [ ] Lock down battle count per run
- [ ] Define total character roster (20? 25? 30?)
- [ ] Map out character distribution across story branches
- [ ] Define main character skill pool
- [ ] Sketch story structure (major branches)
- [ ] Design combat system basics
- [ ] Plan Infinite Mode structure
