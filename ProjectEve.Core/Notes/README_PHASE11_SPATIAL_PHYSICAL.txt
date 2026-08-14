PROJECT EVE — PHASE 11
SPATIAL & PHYSICAL INTERACTION ENGINE

THIS PHASE LOCKS IN THE BEHAVIOR-CUE DESIGN.

A character moving closer or farther away is now world state, not descriptive flavor.
The player is shown the cue and distance change; the game does not explain why the NPC did it.

EXAMPLES

ACTION: move closer to Eve
  4 ft -> 2 ft
  GroupScene transcript receives the resolved physical fact.

NPC ACTION: Eve takes a small step back.
  2 ft -> 5 ft
  Player sees the step and the left PRESENT distance changes.
  No UI label says "Eve is uncomfortable."

ACTION: hug Eve
  Player closes to contact range.
  Contact state = pending.
  UI remains about 1 ft until Eve clearly reciprocates.

Eve: "hugs him back"
  Contact state = active.
  Left PRESENT shows Eve = 0 ft.

Eve: "freezes"
  Contact stays unresolved/frozen.
  0 ft is NOT shown. Freeze is not acceptance.

Eve: "steps back"
  Contact = rejected/withdrawn and physical distance increases.

FIGHT EXAMPLE

ACTION: chest bump Adam
  closes to contact range + pending chest_bump attempt

ACTION: punch Adam
  closes to striking range + punch attempt
  Phase 11 does NOT invent that the punch landed.
  Adam's own Brain can dodge/block/create distance/respond.

This gives us the full fighting vocabulary now without turning the language model into world truth about who got hit. A later combat-resolution layer can resolve damage, knockdown, injury, etc.

VOICE / WHISPER

SAY can now infer delivery from natural text:
  whisper: Don't tell Adam.
  (whispering) Don't tell Adam.
  quietly: I need to ask you something.
  shouting: Get out!

Or ACTION can say:
  whisper to Eve
  lean in and whisper in Eve's ear

The resulting Phase 6 hearing rules still decide who actually hears it.

DISTANCE BANDS

0 ft      active physical contact (display rule)
1-2 ft    very_close
3-5 ft    conversation
6-10 ft   nearby
11-20 ft  across_room
20+ ft    distant

Internally active contact normally keeps about 1 ft of coordinate separation. 0 ft is the UI/world semantic for "touching/contact is active."

CONTACT VOCABULARY

The Core contract includes a much larger extensible vocabulary covering:
- normal/social touch
- romantic/affectionate touch
- intimidation
- striking
- grappling/restraint
- adult intimate physical-state categories

Contact type and reaction are separate. A label such as kiss/hug/grab/punch is not itself acceptance or proof that the action completed.

INTERNAL REACTION STATES

unknown
pending
welcomed
mutual
hesitant
frozen
withdrawn
refused
avoided
interrupted

These are INTERNAL WORLD STATE. PhoneOS does not show them to the player. The player sees behavior.

DATABASE

SceneSpatialInteractionEvent
  permanent provenance of original action -> resolved physical fact

ScenePhysicalContact
  pairwise pending/active/broken/rejected physical-contact state

WHAT PHASE 11 DOES NOT DO YET

- damage / hit points / injuries / knockout resolution
- forced movement through walls/doors
- furniture collision / room polygons
- adult age-validation across PlayerProfile (NPC/world guard should be added before adult mechanics are exposed as gameplay)
- world schedule occupancy (still the next world-movement layer)

NEXT NATURAL PHASE

Phase 12 should be WORLD OCCUPANCY + SCHEDULE MOVEMENT:
job/schedule/plans -> travel -> location -> shared scene.

Then a later combat-resolution phase can sit on top of this spatial engine instead of inventing its own distance/contact model.
