# Story Structure & Branching

## Five Chapter Structure

**Story Progression:**
- **5 Chapters** to complete the main storyline
- **3 Runs per Chapter** (a "run" is a sequence of battles)
- **3 Battles per Run**
- Each chapter concludes with:
  - A new character joins the party
  - A moral decision that cuts off certain characters and future storyline options
  - All characters gain a new skill

## Character Progression Through Story

**Chapter 1:**
- Start with main character
- 3 runs × 3 battles = 9 battles
- End: 1st companion joins, moral decision, all gain skill #1

**Chapter 2:**
- Party of 2 (main + 1 companion)
- 3 runs × 3 battles = 9 battles
- End: 2nd companion joins, moral decision, all gain skill #2

**Chapter 3:**
- Party of 3
- 3 runs × 3 battles = 9 battles
- End: 3rd companion joins, moral decision, all gain skill #3

**Chapter 4:**
- Party of 4
- 3 runs × 3 battles = 9 battles
- End: 4th companion joins, moral decision, all gain skill #4

**Chapter 5:**
- Party of 5 (main + 4 companions)
- 3 runs × 3 battles = 9 battles
- Focus: Gathering final resources for portal key
- End: 5th companion joins, craft portal key, unlock portal, escape to Nexus
- All gain skill #5, unlock Infinite Mode

**Total First Playthrough:**
- 15 runs (5 chapters × 3 runs)
- 45 battles total
- **6 characters in party** (main + 5 companions)
- 5 skills unlocked per character
- Moral decisions at end of each chapter shape available paths
- Dialog decisions between battles create additional branching
- Quest and shop choices affect available equipment and rewards

## Starting Character System

**Choose Starting Character:**
- **Choose starting character from each of 6 factions**
- Creates 6 different entry points into the story
- Story traversal differs based on starting choice
- Different cadence for each playthrough
- Reduces repetitive/linear feeling

**[TO BE DETERMINED]:**
- Is starting character the "main character" or first companion?
- If first companion: who is the actual main character?
- How does starting choice affect skill progression?

## Between-Battle Flow

**After Each Battle:**
1. **Summary/Stats Screen** - Battle performance metrics
2. **Rewards** - Items, currency, quest completion
3. **Skill Development** - Choose skills to improve
4. **Looting** - Collect items from battle
5. **Dialog Sequence** - Story progression

**Dialog Sequences:**
- Determine which battle loads next
- Create branching between battles (story not on rails)
- May have secondary effects (character relationships, faction standing)
- Decision points that impact future interactions
- Example: Choose to help Character A or Character B
  - Affects future availability/relationships with both characters

**Strategic Layer:**
- Not every run follows same path
- Player navigates story network through choices
- Different paths lead to different shops, quests, characters

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

## Shops & Quests

### Shop System
- Strategically placed throughout storyline
- Different shops sell different gear
- **Deterministic inventory** - no random items
- Shops also sell crafting materials for portal keys
- Choosing specific storyline paths to reach certain shops = strategic choice
- Part of overall build strategy

### Quest System
- Items available as quest rewards
- **Portal key resources** available through specific questlines
- **Cannot complete all quests in single run**
- Must choose which quests to prioritize
- Different questlines lead to different portals

### The Portal Quest (Chapter 5 Focus)
- Final chapter focuses on gathering resources for portal key
- Different story paths = different portal locations and recipes
- Resources obtained through:
  - Quest completion
  - Battle rewards
  - Shop purchases
- Craft the key, unlock the portal, escape to Nexus

### Strategic Tension
- Getting specific character + their best weapon/gear = challenging
- Pursuing portal resources vs character optimization
- Shop access vs quest rewards vs character recruitment = competing priorities
- Which portal questline to follow

## Story Flexibility Analysis

**The Challenge:**
- 3 battles per run × 3 runs per chapter × 5 chapters = **45 battle nodes per playthrough**
- Need to recruit 5 companions per playthrough (1 per chapter)
- 30+ total characters to unlock across all playthroughs
- 6 different starting points (faction starting characters)

**Is this structure sufficient for variety?**

### Solutions for Variety

**Shared Battle Pool:**
- Many battles accessible from multiple story paths
- Same battle content, different narrative context
- Recruitment opportunities vary by path taken
- 45 battles per playthrough from pool of 60-80 unique battles

**Dense Branching Within Runs:**
- Each run has 2-3 optional battle branches
- Player chooses which battles to pursue
- Not all content seen in one playthrough
- Still maintain ~3 battles per run pacing

**Chapter-Specific Design:**
- Early chapters (1-2): More linear (tutorial, introduce mechanics)
- Later chapters (3-5): Heavy branching for character recruitment
- Front-loaded teaching, back-loaded variety

**Starting Point Divergence:**
- Each of 6 starting characters has unique path elements
- Paths converge at key story moments
- Ensures meaningful differences between playthroughs

**Likely Implementation:**
- Use branching between battles (dialog choices)
- Chapter-end moral decisions gate major paths
- Starting character determines initial trajectory
- Some battles reused with different context
- Result: Players experience 45 battles per playthrough from pool of **75-100 unique battles**

### Character Gating Strategy
- 5 recruitment points per playthrough (one per chapter)
- If 6 starting options, ~6 different characters available per chapter base
- With branching: 8-10 possible recruits per chapter, only 3-4 accessible per path
- Strategic gating: earlier decisions determine later availability
- Example: Help Faction A in Chapter 1 → unlock Faction A characters in Chapters 3-5
- This approach enables 40-50+ total characters without needing 30+ unique chapter paths

## Open Questions

### Story Structure
1. Total number of unique battle nodes? (75-100?)
2. How many decision points per run? (1-2?)
3. How many "hub" points where branches rejoin?
4. Exact nature of moral decisions (faction allegiances? ethical dilemmas? pragmatic choices?)

### World Building
1. Faction relationships: Alliances? Conflicts? Neutral parties?
2. How do story branches maintain coherent narrative across 6 starting points?
3. World scope: Single region? Multiple kingdoms? Contained or expansive?
4. How do multiple completions of storyline work narratively?

### Tutorial & Onboarding
1. Chapter 1 teaching: How to introduce all mechanics with solo main character?
2. Complexity scaling: When to introduce advanced mechanics (crafting, synergies, etc.)?
3. First-time player experience: How much guidance vs discovery?
4. New Game+ differences: Can skip tutorial? Faster progression?
