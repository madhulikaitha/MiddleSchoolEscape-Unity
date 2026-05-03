# Hall Clash — Complete Game Build Plan (v2)
### Unity 2D Top-Down | Single Player | Shared Leaderboard | 1-Week Build

---

## Game Overview

**Title:** Hall Clash  
**Genre:** 2D Top-Down Challenge Runner  
**Players:** 1 player per session, competing against all other students on a shared leaderboard  
**Platform:** PC (Windows build, run on a shared school computer)  
**Engine:** Unity 2D (any recent LTS version)  
**Camera:** Top-down overhead view (like early Zelda or Hotline Miami)

**Concept:** You are a new student at Maplewood Middle School — late on your very first day. You customize your character, rush through a quick tutorial in the school lobby, then sprint through 3 chaotic challenges to reach class before the final bell. Your total time is saved to a shared leaderboard that all students compete on. Everyone is trying to beat each other's time.

**Story premise (told through UI text, no cutscenes):**
> *"First day at Maplewood Middle. You overslept. The bus left without you. You sprinted here and made it through the front door — but class started 3 minutes ago. Principal Chen is watching the cameras. Every student, every teacher, every janitor is in your way. How fast can you make it to Room 204?"*

Each challenge is one chaotic section of the school you have to navigate to reach your classroom. The leaderboard shows who got there fastest across all students who have played.

---

## Scene List

| Scene Name | Purpose |
|---|---|
| MainMenu | Title screen with Play, View Leaderboard, Quit |
| CharacterCustomization | Choose skin tone, hair, uniform, enter name |
| Tutorial | School lobby — teaches top-down movement and dodge mechanics |
| Level1_Cafeteria | Challenge 1 — Cafeteria Chaos (top-down) |
| Level2_Hallway | Challenge 2 — Hallway Gauntlet (top-down) |
| Level3_ScienceLab | Challenge 3 — Science Lab Meltdown (top-down) |
| RunComplete | Shows player's individual time breakdown, saves to leaderboard |
| Leaderboard | Top 10 all-time times across all students, "Play Again" button |

---

## Controls (Top-Down)

| Action | Keyboard | Controller |
|---|---|---|
| Move up | W or Up Arrow | Left stick up / D-pad up |
| Move down | S or Down Arrow | Left stick down / D-pad down |
| Move left | A or Left Arrow | Left stick left / D-pad left |
| Move right | D or Right Arrow | Left stick right / D-pad right |
| Dash / dodge roll | Space or Shift | A button |
| Interact (tutorial only) | E | X button |
| Pause | Escape | Start |

**Movement note:** Top-down 8-directional movement using a Rigidbody2D with no gravity. Player faces the direction they are moving (rotate sprite to match velocity direction).

---

## Character Customization Design

Players build their Maplewood student before playing. All layers are separate sprites composited on a single character rig.

### Customization options:

1. **Skin tone** — 5 options
   - Light (e.g. #FDDBB4)
   - Medium-light (e.g. #E8B88A)
   - Medium (e.g. #C68642)
   - Medium-dark (e.g. #8D5524)
   - Dark (e.g. #4A2912)
   - Implemented as a Color tint on the body/face sprite layer

2. **Hair style** — 6 options
   - Short straight
   - Long straight
   - Curly afro
   - Braids
   - Ponytail
   - Buzz cut
   - Each is a separate sprite layer rendered above the head

3. **School uniform** — 4 options (all are school uniforms, just different colors/styles)
   - Standard navy blue uniform
   - Green and grey uniform
   - Red and white uniform
   - Purple and black uniform
   - Each is a full-body sprite swap (not a tint — drawn separately)

4. **Name** — Text input field. Shown above character in game and on leaderboard.

### PlayerData class:
```csharp
public static class PlayerData {
    public static string playerName = "Player";
    public static int skinToneIndex = 0;    // 0–4
    public static int hairIndex = 0;         // 0–5
    public static int uniformIndex = 0;      // 0–3
    public static float timeLevel1 = 0f;
    public static float timeLevel2 = 0f;
    public static float timeLevel3 = 0f;
    public static float TotalTime() => timeLevel1 + timeLevel2 + timeLevel3;
}
```

### UI layout:
- Left panel: character preview (centered, large, shows live updates)
- Right panel: category tabs — Skin | Hair | Uniform
- Arrow buttons to cycle through options within each category
- Name field at the bottom
- "Start Game" button → loads Tutorial

---

## Tutorial Level — School Lobby (Top-Down)

**Story context:** *"You've just burst through the front doors. The lobby is quiet. A sign says: 'Welcome to Maplewood.' A hall monitor points you to the stairwell."*

**Camera:** Top-down. Player sees the lobby from above — reception desk, benches, trophy cases along the walls.  
**Length:** ~45 seconds at a relaxed pace  
**No timer. No fail state.**

### Tutorial zone sequence:
1. **Spawn point:** Front door. Pop-up: *"Use W/A/S/D to move"* — player must reach the reception desk to continue.
2. **Dodge zone:** A rolling cart moves slowly across the lobby. Pop-up: *"Press Space to dash out of the way!"*
3. **Narrow corridor:** Two benches create a tight gap. Pop-up: *"Navigate tight spaces carefully — bumping obstacles slows you down."*
4. **Janitor hazard:** Janitor pushes a mop bucket across the path. Pop-up: *"Watch out for moving hazards — they slow you on contact."*
5. **Stairwell door:** Pop-up: *"You're ready! Get to class before Principal Chen catches you!"* — door opens, loads Level 1.

---

## Challenge 1 — Cafeteria Chaos (Top-Down)

**Story context:** *"The fastest route to Room 204 cuts straight through the cafeteria. Lunch just started. It's absolute chaos."*

**View:** Top-down. Player sees cafeteria tables, food stations, and students from above.  
**Length:** ~30–45 seconds on a good run  
**Unique mechanic:** Lunch lady NPC patrols and launches food projectiles in random directions

### Level layout (navigate from south entrance to north exit):
1. **Start trigger:** South cafeteria doors — timer begins
2. **Zone A — Table maze:** 6 rectangular tables arranged to create winding paths. Player navigates between them top-down.
3. **Zone B — Food tray gauntlet:** 4 food trays slide rapidly across open floor space in horizontal lines (top-down sliding obstacles)
4. **Zone C — Spill hazards:** 3 puddles of spilled milk on the floor — top-down circular hazard zones. Walking through slows speed by 50% for 1.5 seconds.
5. **Zone D — Lunch lady patrol:** Lunch lady NPC walks a patrol path. Every 2–4 seconds (randomized), she launches a food projectile outward in a random direction. Getting hit = 2.5 second stun.
6. **Finish trigger:** North cafeteria doors — timer stops, time saved

### Key scripts:

**FoodTray (top-down sliding obstacle):**
```csharp
public class FoodTray : MonoBehaviour {
    public float speed = 5f;
    public float range = 6f;
    private Vector3 startPos;
    private int direction = 1;

    void Start() { startPos = transform.position; }

    void Update() {
        transform.Translate(Vector2.right * speed * direction * Time.deltaTime);
        if (Mathf.Abs(transform.position.x - startPos.x) >= range)
            direction *= -1;
    }
}
```

**SpillPuddle (slow zone):**
```csharp
public class SpillPuddle : MonoBehaviour {
    void OnTriggerStay2D(Collider2D other) {
        if (other.CompareTag("Player"))
            other.GetComponent<PlayerController>().SetSpeedMultiplier(0.5f);
    }
    void OnTriggerExit2D(Collider2D other) {
        if (other.CompareTag("Player"))
            other.GetComponent<PlayerController>().SetSpeedMultiplier(1f);
    }
}
```

**LunchLady (random projectile):**
```csharp
public class LunchLady : MonoBehaviour {
    public GameObject foodProjectilePrefab;
    private float timer, nextThrow;
    void Start() { nextThrow = Random.Range(2f, 4f); }
    void Update() {
        timer += Time.deltaTime;
        if (timer >= nextThrow) {
            float angle = Random.Range(0f, 360f);
            Vector2 dir = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle));
            GameObject proj = Instantiate(foodProjectilePrefab, transform.position, Quaternion.identity);
            proj.GetComponent<Rigidbody2D>().velocity = dir * 6f;
            timer = 0;
            nextThrow = Random.Range(2f, 4f);
        }
    }
}
```

---

## Challenge 2 — Hallway Gauntlet (Top-Down)

**Story context:** *"The bell rang. 800 students just flooded the hallway at once. You need to cut through to the east stairwell. Good luck."*

**View:** Top-down long corridor. Player sees lockers lining both walls, students moving in all directions.  
**Length:** ~35–50 seconds on a good run  
**Unique mechanic:** Dropped backpacks on the floor act as obstacles — walk into them and you're slowed for 2 seconds

### Level layout (navigate west entrance to east stairwell door):
1. **Start trigger:** West hallway entrance — timer begins
2. **Zone A — Locker gauntlet:** 5 locker doors randomly swing open outward into the corridor (top-down: locker doors extend perpendicular from wall). Timing-based dodge.
3. **Zone B — Student crowd:** 8 NPC students walk in random directions. Bumping into one = 1 second stun. Player must weave through the crowd.
4. **Zone C — Backpack field:** 6 backpacks scattered on the floor as static obstacles. Touching one = 2 second speed debuff (60% speed). Backpacks disappear after touched.
5. **Zone D — Hall monitor patrol:** Hall monitor walks a fixed route. Has a forward-facing vision cone (triangular trigger). If player enters cone → monitor chases at 80% of player max speed for 4 seconds. Being caught = 3 second freeze penalty.
6. **Finish trigger:** East stairwell door — timer stops

### Key scripts:

**LockerDoor (top-down swing):**
```csharp
public class LockerDoor : MonoBehaviour {
    public float openDuration = 1.5f;
    public float closedDuration = 2.5f;
    void Start() { StartCoroutine(LockerCycle()); }
    IEnumerator LockerCycle() {
        while (true) {
            GetComponent<Collider2D>().enabled = true;
            GetComponent<Animator>().SetBool("isOpen", true);
            yield return new WaitForSeconds(openDuration);
            GetComponent<Collider2D>().enabled = false;
            GetComponent<Animator>().SetBool("isOpen", false);
            yield return new WaitForSeconds(closedDuration);
        }
    }
}
```

**HallMonitor (vision cone chase):**
```csharp
public class HallMonitor : MonoBehaviour {
    public float chaseSpeed = 3.5f;
    public float chaseDuration = 4f;
    private Transform player;
    private bool isChasing = false;

    void OnTriggerEnter2D(Collider2D other) {
        if (other.CompareTag("Player") && !isChasing)
            StartCoroutine(ChaseRoutine(other.transform));
    }

    IEnumerator ChaseRoutine(Transform target) {
        isChasing = true;
        float t = 0;
        while (t < chaseDuration) {
            transform.position = Vector2.MoveTowards(transform.position, target.position, chaseSpeed * Time.deltaTime);
            t += Time.deltaTime;
            yield return null;
        }
        isChasing = false;
    }
}
```

---

## Challenge 3 — Science Lab Meltdown (Top-Down)

**Story context:** *"The science lab is on your route. Someone knocked over the wrong beaker. The room is filling with strange gas, platforms of lab equipment are collapsing, and the electricity is going haywire."*

**View:** Top-down lab. Player sees lab benches, equipment, gas clouds spreading from center.  
**Length:** ~40–55 seconds on a good run  
**Unique mechanic:** Spreading gas clouds that slowly expand from burst beakers — player must route around them in real time

### Level layout (navigate south lab door to north emergency exit):
1. **Start trigger:** South lab door — timer begins
2. **Zone A — Collapsing benches:** 4 lab benches that disappear on a timer (flash 3 times, then become passable). Player must time crossing them.
3. **Zone B — Electric arcs:** Electrical sparks shoot between two poles in a straight line every 2 seconds. The arc is a brief but wide hazard — touching it = 2 second stun.
4. **Zone C — Spreading gas clouds:** 3 gas clouds slowly expand outward from burst beakers (increase collider radius over time). Routes that were open at the start close off as clouds grow. Player must move fast before all paths close.
5. **Zone D — Beaker minefield:** Rows of fragile beakers on the floor. Running into one triggers a small explosion (visual only) that stuns for 1 second and creates a small temporary gas cloud at that spot.
6. **Finish trigger:** North emergency exit — timer stops, run complete

### Key scripts:

**GasCloud (expanding hazard):**
```csharp
public class GasCloud : MonoBehaviour {
    public float expandRate = 0.3f;
    public float maxRadius = 3f;
    private CircleCollider2D col;
    private SpriteRenderer sr;

    void Start() {
        col = GetComponent<CircleCollider2D>();
        sr = GetComponent<SpriteRenderer>();
    }

    void Update() {
        if (col.radius < maxRadius) {
            col.radius += expandRate * Time.deltaTime;
            float s = col.radius * 2;
            transform.localScale = new Vector3(s, s, 1);
        }
    }

    void OnTriggerEnter2D(Collider2D other) {
        if (other.CompareTag("Player"))
            other.GetComponent<PlayerController>().GetHit(2f);
    }
}
```

**CollapsingBench:**
```csharp
public class CollapsingBench : MonoBehaviour {
    public float visibleTime = 4f;
    public float hiddenTime = 2f;
    void Start() { StartCoroutine(BenchCycle()); }
    IEnumerator BenchCycle() {
        while (true) {
            yield return new WaitForSeconds(visibleTime - 0.6f);
            for (int i = 0; i < 3; i++) {
                GetComponent<SpriteRenderer>().color = Color.red;
                yield return new WaitForSeconds(0.1f);
                GetComponent<SpriteRenderer>().color = Color.white;
                yield return new WaitForSeconds(0.1f);
            }
            GetComponent<Collider2D>().enabled = false;
            GetComponent<SpriteRenderer>().enabled = false;
            yield return new WaitForSeconds(hiddenTime);
            GetComponent<Collider2D>().enabled = true;
            GetComponent<SpriteRenderer>().enabled = true;
        }
    }
}
```

---

## Player Controller (Top-Down)

```csharp
public class PlayerController : MonoBehaviour {
    public float moveSpeed = 5f;
    public float dashSpeed = 12f;
    public float dashDuration = 0.15f;
    private Rigidbody2D rb;
    private bool isDashing = false;
    private bool isStunned = false;
    private float speedMultiplier = 1f;

    void Start() { rb = GetComponent<Rigidbody2D>(); }

    void Update() {
        if (isStunned || isDashing) return;
        float h = Input.GetAxisRaw("Horizontal");
        float v = Input.GetAxisRaw("Vertical");
        Vector2 dir = new Vector2(h, v).normalized;
        rb.velocity = dir * moveSpeed * speedMultiplier;

        // Rotate sprite to face movement direction
        if (dir != Vector2.zero) {
            float angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg - 90f;
            transform.rotation = Quaternion.Euler(0, 0, angle);
        }

        if (Input.GetKeyDown(KeyCode.Space) && dir != Vector2.zero)
            StartCoroutine(Dash(dir));
    }

    IEnumerator Dash(Vector2 dir) {
        isDashing = true;
        rb.velocity = dir * dashSpeed;
        yield return new WaitForSeconds(dashDuration);
        isDashing = false;
    }

    public void GetHit(float stunDuration) {
        if (!isStunned) StartCoroutine(StunRoutine(stunDuration));
    }

    IEnumerator StunRoutine(float duration) {
        isStunned = true;
        rb.velocity = Vector2.zero;
        GetComponent<SpriteRenderer>().color = Color.red;
        yield return new WaitForSeconds(duration);
        GetComponent<SpriteRenderer>().color = Color.white;
        isStunned = false;
    }

    public void SetSpeedMultiplier(float mult) { speedMultiplier = mult; }
}
```

---

## Timer System

```csharp
public class LevelTimer : MonoBehaviour {
    public float elapsedTime = 0f;
    private bool isRunning = false;
    public TextMeshProUGUI timerDisplay;

    public void StartTimer() { isRunning = true; elapsedTime = 0f; }

    public void StopAndSave() {
        isRunning = false;
        string scene = SceneManager.GetActiveScene().name;
        if (scene == "Level1_Cafeteria") PlayerData.timeLevel1 = elapsedTime;
        else if (scene == "Level2_Hallway") PlayerData.timeLevel2 = elapsedTime;
        else if (scene == "Level3_ScienceLab") PlayerData.timeLevel3 = elapsedTime;
    }

    void Update() {
        if (!isRunning) return;
        elapsedTime += Time.deltaTime;
        timerDisplay.text = FormatTime(elapsedTime);
        timerDisplay.color = elapsedTime > 60f ? Color.red : Color.white;
    }

    public static string FormatTime(float t) {
        int mins = (int)(t / 60);
        float secs = t % 60;
        return string.Format("{0:00}:{1:00.00}", mins, secs);
    }
}
```

---

## Shared Leaderboard System

All students play on the same computer. Leaderboard persists between sessions using `PlayerPrefs`. Top 10 all-time times are shown.

```csharp
public class LeaderboardManager : MonoBehaviour {
    private const int MAX_ENTRIES = 10;

    public void SaveScore(string name, float totalTime) {
        var scores = LoadScores();
        scores.Add((name, totalTime));
        scores.Sort((a, b) => a.Item2.CompareTo(b.Item2));
        if (scores.Count > MAX_ENTRIES)
            scores.RemoveRange(MAX_ENTRIES, scores.Count - MAX_ENTRIES);
        for (int i = 0; i < scores.Count; i++) {
            PlayerPrefs.SetString("LB_Name_" + i, scores[i].Item1);
            PlayerPrefs.SetFloat("LB_Time_" + i, scores[i].Item2);
        }
        PlayerPrefs.SetInt("LB_Count", scores.Count);
        PlayerPrefs.Save();
    }

    public List<(string, float)> LoadScores() {
        var scores = new List<(string, float)>();
        int count = PlayerPrefs.GetInt("LB_Count", 0);
        for (int i = 0; i < count; i++) {
            string n = PlayerPrefs.GetString("LB_Name_" + i, "---");
            float t = PlayerPrefs.GetFloat("LB_Time_" + i, 0f);
            scores.Add((n, t));
        }
        return scores;
    }
}
```

**RunComplete scene logic:**
- Show breakdown: Level 1 time | Level 2 time | Level 3 time | Total
- Highlight which level was the player's worst (encourage improvement)
- Show where they rank: "You placed #3 on the leaderboard!"
- Buttons: "View Leaderboard" | "Play Again" | "Main Menu"

---

## Scene Flow

```
MainMenu
  → [Play] → CharacterCustomization
               → [Start] → Tutorial
                             → [Exit lobby] → Level1_Cafeteria
                                               → [North doors] → Level2_Hallway
                                                                   → [East stairs] → Level3_ScienceLab
                                                                                       → [Emergency exit] → RunComplete
                                                                                                              → [View LB] → Leaderboard
                                                                                                              → [Play Again] → CharacterCustomization
                                                                                                              → [Menu] → MainMenu
  → [Leaderboard] → Leaderboard
                      → [Play] → CharacterCustomization
  → [Quit] → Application.Quit()
```

---

## 7-Day Build Schedule

### Day 1 — Project Foundation + Top-Down Movement
- Create Unity 2D project, set up all 8 scenes in Build Settings
- Folder structure: Scripts/ Sprites/ Prefabs/ Scenes/ Audio/ UI/
- Write `PlayerController.cs` — 8-directional top-down movement, dash, stun system
- Write `PlayerData.cs` static class
- Set up camera: top-down orthographic, Cinemachine follow
- Placeholder: colored square as player, confirm movement and dash feel great
- Confirm Rigidbody2D gravity scale = 0 (no gravity in top-down)

### Day 2 — Character Customization
- Create layered character sprite system: body, face, hair layer, uniform layer
- Draw or source: 5 skin tone body/face sprites, 6 hair sprites, 4 uniform sprites
- Build CharacterCustomization UI: large preview + category tabs (Skin / Hair / Uniform)
- Write `CharacterCustomizationManager.cs`
- Add name input field (TextMeshPro)
- Save all to `PlayerData`, "Start" button loads Tutorial
- **Hard stop: do not add more than what is listed above**

### Day 3 — Tutorial Level
- Design top-down lobby tilemap: reception desk, benches, trophy cases, walls
- 5 trigger zones with TextMeshPro pop-up panels (move, dash, narrow space, hazard, exit)
- Add rolling cart NPC (simple back-and-forth across lobby)
- Add janitor NPC (slow patrol, bumping causes brief slow)
- Write `TutorialZone.cs`
- Exit door → Level1_Cafeteria

### Day 4 — Challenge 1: Cafeteria Chaos
- Design top-down cafeteria tilemap
- `LevelTimer.cs` — start trigger at south doors, stop trigger at north doors
- `FoodTray.cs` — 4 sliding trays
- `SpillPuddle.cs` — 3 slow zones
- `LunchLady.cs` — random directional projectile throw
- Food projectile prefab with `OnTriggerEnter2D` → `player.GetHit(2.5f)`
- Playtest and tune obstacle speed / tray timing

### Day 5 — Challenges 2 & 3
**Morning — Hallway Gauntlet:**
- Top-down hallway tilemap (long corridor, lockers on both sides)
- `LockerDoor.cs` — 5 doors on staggered timers
- NPC student crowd — 8 wandering NPCs using simple random walk
- `Backpack.cs` — 6 floor items with speed debuff
- `HallMonitor.cs` — patrol + vision cone trigger + chase

**Afternoon — Science Lab Meltdown:**
- Top-down lab tilemap (benches in grid layout, equipment on benches)
- `CollapsingBench.cs` — 4 benches with flash-then-disappear
- Electric arc — static sprite that flashes on/off every 2s, has trigger collider when active
- `GasCloud.cs` — 3 expanding cloud hazards
- Beaker obstacles — static triggers that stun + spawn small temporary gas cloud on contact

### Day 6 — Leaderboard, RunComplete, Full Flow
- `LeaderboardManager.cs` using PlayerPrefs
- RunComplete scene UI — time breakdown table, rank reveal, 3 buttons
- Leaderboard scene UI — top 10 table with rank, name, total time, per-level times
- Highlight current player's entry in gold if they appear in top 10
- Main Menu scene — Play, Leaderboard, Quit buttons with school logo art
- Wire all scene transitions, test complete run end-to-end

### Day 7 — Polish + Playtesting
**Audio:**
- Background music: upbeat/chaotic for cafeteria, fast-paced for hallway, tense/eerie for lab
- SFX: footstep (looping), dash whoosh, stun hit, level complete jingle, leaderboard fanfare

**Visual juice:**
- Screen shake on stun hit
- Player flashes red briefly when hit
- Dash leaves a brief trail/ghost effect (set previous position sprite to 30% alpha, destroy after 0.1s)
- Gas cloud has gentle pulse animation (scale up/down slightly)
- Timer turns orange at 45s, red at 60s

**Final QA checklist:**
- [ ] All 3 levels completable start to finish
- [ ] Timer records correctly per level and totals correctly
- [ ] Leaderboard saves between sessions (close and reopen the game to verify)
- [ ] Character customization (skin, hair, uniform) shows correctly in all 3 levels
- [ ] Tutorial teaches all necessary mechanics
- [ ] "Play Again" flow works correctly — new customization, fresh times
- [ ] No player clipping through walls
- [ ] Game runs smoothly on school hardware
- [ ] Build exported as Windows .exe

---

## Scope Rules

1. **No more than 4 customization categories** — skin, hair, uniform, name. Hard stop.
2. **No cutscenes** — story delivered through UI text panels only.
3. **No online leaderboard** — local PlayerPrefs only. One shared PC per classroom session.
4. **No animations beyond idle/walk/stun** — 2–4 frame sprite sheets maximum.
5. **Exactly 3 levels** — do not add a 4th, no matter how tempting.
6. **No dialogue trees** — NPCs react with simple behavior only.
7. **Restart on death** — no game over screen, just a brief "Stunned!" UI flash and continue.

---

## Cursor Tips

- Always tell Cursor: *"This is a top-down 2D Unity game. The Rigidbody2D has gravity scale = 0."*
- When generating NPC scripts, specify: *"NPC uses Rigidbody2D MovePosition, not transform.Translate"*
- Paste full Unity Console errors directly into Cursor for fast debugging
- Ask Cursor to generate complete scripts, not snippets — it handles Unity's API well

---

*Hall Clash v2 | Top-Down 2D | Unity | Shared Leaderboard | 1-Week Build*
