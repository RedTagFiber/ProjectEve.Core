PROJECT EVE PHONEOS — SERVER MESSAGING v2
=========================================

BUILT AGAINST YOUR UPLOADED ProjectEve.PhoneOS.zip.

THIS ZIP ALSO INCLUDES THE RESPONSIVE v1 FILES
-----------------------------------------------
So you do not need to stack the old visual patch first.

PHONE BEHAVIOR NOW
------------------
OLD:
    type message
    -> Messages.razor waits on Chat.GetReplyAsync()
    -> page is effectively tied to the AI call

NEW:
    type message
    -> exact player message saves immediately to laptop SQLite
    -> reply is queued on the laptop/server
    -> page stays usable
    -> background worker later calls Eve Brain
    -> exact Eve reply saves to SQLite
    -> page polls the server once per second and displays it

If you navigate away from Messages after sending:
    THE LAPTOP SERVER STILL OWNS THE QUEUED MESSAGE.
The background reply worker keeps running.
When you return to Messages, stored messages are loaded again.

THIS IS THE FIRST IMPORTANT SERVER-FIRST STEP.

NEW FILE
--------
ProjectEve.PhoneOS/Components/Services/PhoneMessagingService.cs

REPLACED FILES
--------------
ProjectEve.PhoneOS/Components/Pages/Messages.razor
ProjectEve.PhoneOS/Program.cs
ProjectEve.PhoneOS/Components/Layout/MainLayout.razor
ProjectEve.PhoneOS/wwwroot/app.css

CONTACT RULE
------------
PhoneMessagingService contains PlayerPhoneContact.
Contacts are keyed by:
    PlayerId + NpcId

It does NOT populate 200 town NPCs.

The Eve Messages page calls EnsureKnownContact only because opening the existing
Eve text thread means the player legitimately has an Eve phone connection.

Later the contact-acquisition system should create contacts only when:
- number exchanged
- NPC texted player
- player legitimately obtained number
- business/public contact is known

NPC AVAILABILITY / WORK
-----------------------
Internal availability is NOT shown in the UI.

The UI does NOT say:
    "Eve is at work"
    "Eve is busy"
    "Eve is sleeping"

The player only experiences response timing.

v2 uses a SHORT TEST delay (normally about 6-23 seconds) so you can prove the
queue works without waiting half an hour while developing.

This is NOT the final human scheduling model.

The final ProjectEve server decision should use:
- job activity right now
- phone access
- workload
- sleep
- driving/emergency state
- current location
- who the NPC is with
- relationship with sender
- message importance
- mood
- personality
- whether the NPC wants to answer

WORK MUST NOT MEAN "NO TEXT".
Work can create a short, medium, or long delay depending on what the NPC is doing.

PLAYER ID
---------
All new phone tables already include PlayerId.
That is intentional for future one/two-player support.

Current PlayerProfileService still has the old single-profile persistence model.
So the DATABASE is PlayerId-ready, but true simultaneous-player identity/circuit
separation is a later patch.

DATABASE
--------
Default:
    D:\ProjectEve\EveData\db\phone_messages.db

Fallback:
    <PhoneOS app>/Data/phone_messages.db

Override:
    EVE_PHONE_DB_PATH

Tables:
    PlayerPhoneContact
    PhoneThread
    PhoneMessage

PhoneMessage stores the exact transcript lines.
The Razor page no longer owns the conversation history.

IMPORTANT — CONVERSATION EVENT MEMORY
-------------------------------------
This patch persists the exact phone transcript and queues replies.

The separate ConversationManager system we already built is still the next
integration layer for:
    active conversation section
    full section sent to Thought/Dialogue
    section-end summary
    ConversationEvent
    ConversationFact
    ConversationPlan
    text -> in-person continuity

I did not fake that connection inside PhoneMessagingService because the current
IEveChatService only exposes GetReplyAsync(channel,text). We need to connect the
actual Brain/ProjectEve side cleanly so Thought receives the whole active section.

TEST
----
1. Copy the ProjectEve.PhoneOS folder over the actual project.
2. Rebuild/run.
3. Open Messages.
4. Send:
       hey eve how was work
5. Your blue message should appear immediately.
6. The page should NOT freeze waiting for Eve.
7. You should see only "Message delivered" while a reply is pending.
8. Eve's response should appear later when the server worker finishes it.
9. Send another message and immediately go Home.
10. Wait a little, then return to Messages.
11. The saved Eve response should still be there.

That last test proves the page no longer owns the reply lifecycle.

LAN
---
The responsive/LAN hook from v1 remains included.

Example:
    set EVE_PHONEOS_URLS=http://0.0.0.0:5055

Then later a phone on your LAN can connect to the laptop server.

NEXT PATCH
----------
1. Wire ConversationManager into actual Brain/IEveChatService path.
2. Give Thought + Dialogue the WHOLE active section.
3. Add section closing -> summary/event/facts/plans.
4. Add real ProjectEve availability/willingness scheduling.
5. Add contact/thread list UI instead of Messages being Eve-only.
6. Split player identity correctly for two simultaneous players.
7. Add SignalR/event push so the UI does not need one-second polling.
