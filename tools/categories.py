"""Single source for Common/Defs/ThoughtCategories.xml.
Usage: python3 tools/categories.py simulate <thoughts.tsv>   prints which category every thought lands in
       python3 tools/categories.py write <path to ThoughtCategories.xml>"""
import csv
import sys
from xml.sax.saxutils import escape

C = []


def cat(order, id, label, desc, prio, names=(), pats=(), files=(), precepts=False, traits=False, social=False, fallback=False):
    C.append(dict(order=order, id=id, label=label, desc=desc, prio=prio, names=list(names), pats=list(pats), files=list(files),
                  precepts=precepts, traits=traits, social=social, fallback=fallback))


cat(1, "death", "death and loss", "Deaths, kidnappings and other losses of family, friends and bonded animals.", 80,
    names=["KilledChild", "KnowBuriedInSarcophagus"],
    pats=["Died", "Lost", "WitnessedDeath", "FailedToRescue", "PsychicBondTorn", "BondedAnimalReleased"],
    files=["Memory_Death", "Memory_Lost"])
cat(2, "justice", "prisoners and justice", "Executions, organ harvesting, prisoners, slaves, banishment and trials.", 90,
    pats=["Executed", "Prisoner", "Slave", "Enslaved", "Imprisoned", "Banished", "OrganHarvested", "Trial", "Arrested", "SurgicallyViolated", "XenogermHarvested"])
cat(3, "corpses", "corpses and gore", "Seeing, smelling, butchering or wearing the dead, and terror buildings like gibbets and skullspikes.", 85,
    pats=["Corpse", "RotStink", "LeftUnburied", "Gibbet", "Skullspike", "ObservedTerror", "DeadMansApparel", "HumanLeatherApparel", "DreadLeather"])
cat(4, "food", "food and eating", "What was eaten and how: raw food, fine meals, eating without a table.", 60,
    pats=["AteWithoutTable", "AteInImpressive", "AteRaw", "AteFine", "AteLavish", "AteKibble", "AteInsect", "AteRotten", "AteTwisted", "AteHumanlike", "AteCorpse"],
    files=["Memory_Eating"])
cat(5, "rest", "sleep and bedrooms", "Sleeping outside, on the ground, in the cold, in barracks, and bedroom quality.", 70,
    names=["SleptInRoomWithSlave"],
    pats=["Slept", "SleepDisturbed", "Bedroom", "Barracks", "Deathrest", "SharedBed", "WantToSleepWith"])
cat(6, "needs", "unmet needs", "The always-on thoughts from hunger, tiredness, recreation, beauty, comfort, space and outdoors.", 50,
    pats=["Need"],
    files=["Situation_Needs"])
cat(7, "drugs", "drugs", "Highs, withdrawal and drug cravings. No preset touches these.", 75,
    names=["TeetotalerVsAddict", "TeetotalerVsChemicalInterest", "ChemicalInterestVsTeetotaler"],
    pats=["DrugDesire", "Withdrawal", "Addict", "High"])
cat(8, "environment", "environment and comfort", "Temperature, darkness, getting wet, nudity, damaged clothes, rooms, weather and music.", 40,
    names=["PrisonBarracks"],
    pats=["Environment", "Apparel", "SoakingWet", "Naked", "ToxicFallout", "NoxiousHaze", "Aurora", "ListeningTo", "PrisonCell", "RoomStats", "ImpressiveRecRoom", "WearingColor"],
    files=["RoomStats", "MusicalInstruments"])
cat(9, "social", "social", "Opinions of other pawns and the mood from fights, insults, kind words and chats.", 30,
    names=["KilledMyLover", "KilledMyFiance", "KilledMySpouse", "KilledChild_Opinion", "HealedMe"],
    pats=["Insult", "Slight", "Fight", "KilledMy", "HarmedMe", "KindWords", "Chitchat", "DeepTalk", "RapportBuilt", "Counsel"],
    files=["Memory_Social", "Situation_Social", "Situational_Social"],
    social=True)
cat(10, "romance", "romance", "Lovers, breakups, cheating, divorce, proposals and lovin'.", 78,
    pats=["Romance", "BrokeUp", "Cheated", "Divorce", "Proposal", "Lovin", "Lover", "Honeymoon", "Affair", "Incest"])
cat(11, "health", "health and pain", "Pain, sickness, pregnancy and the health side of genes and implants.", 65,
    names=["AnestheticHigh", "PregnancyMood_High"],
    pats=["Pain", "Sick", "Pregnan", "Miscarried", "Stillbirth", "HemogenCraving", "SunlightSensitivity", "Joywire", "Joyfuzz", "TendedBy", "MissingTongue", "Biosculpter", "FedOn", "IngestedHemogen"])
cat(12, "traits", "traits and genes", "Thoughts that only exist because of a trait or gene: optimist, depressive, pyromaniac, night owl and so on.", 67,
    names=["WitnessedDeathBloodlust", "HumanLeatherApparelHappy", "ChildlikePositivity"],
    pats=["MoodOffset", "Bloodlust", "PyroUsed", "Pyromaniac", "Recluse", "NightOwl", "Undergrounder", "Prosthoph", "Masochist", "Ascetic", "Greedy", "Jealous", "Nudist", "HappyNude", "Brawler", "KillThirst", "BloodRage", "LookChangeDesired"],
    files=["Situation_Traits", "Situation_Genes"],
    traits=True)
cat(13, "psychic", "psychic and anomaly", "Psychic drones and soothes, the anima tree, archotech emanators and everything from the void.", 72,
    names=["ImprisonedWithEntity", "UnnaturalCorpse", "UnnaturalCorpseDestroyed"],
    pats=["Psychic", "Void", "Unnatural", "Cube", "Ghoul", "AnimaScream", "Archotech", "Neuroquake", "Obelisk", "Entity", "Flesh", "HateChant", "HateAura", "Harbinger", "GrayPall", "DeathPall", "DeadlifeDust", "Bioferrite", "Inhuman", "Darkness", "Tome", "Disturbing", "JoyousPresence", "PleasurePulse", "NeurosisPulse", "JuggernautSerum", "Mutated", "Chimera", "BlissLobotomy"])
cat(14, "gatherings", "gatherings and rituals", "Parties, weddings, concerts, speeches, funerals and every ideology ritual outcome.", 45,
    pats=["Party", "Concert", "Wedding", "Married", "Speech", "Festival", "Funeral", "Duel", "SkyLanterns", "BestowingCeremony", "Catharsis", "Execution", "Sacrifice", "Blinding", "Scarification", "Conversion", "RelicAtRitual"],
    files=["Ritual", "Gatherings"])
cat(15, "colony", "colony life", "Good things about the colony itself: a new start, rescues, victories, relics, passions and pets.", 35,
    names=["LoveReading"],
    pats=["NewColony", "Rescued", "DefeatedHostile", "DefeatedInsect", "DefeatedMech", "TravelAnticipation", "Relic", "Artifact", "Nuzzled", "BondedAnimalMaster", "PassionateWork", "LoveReading", "SkillBook", "OnDuty"])
cat(16, "children", "children and babies", "Crying and giggling babies, cribs, growth vats, and how parents feel about their kids.", 70,
    pats=["Baby", "Child", "Youngsters", "MyParent", "Crib", "Breastfed", "PlayedWithMe", "GaveLesson", "WasTaught", "FedMe"],
    files=["Children", "Childcare"])
cat(17, "titles", "royal titles and roles", "Royal title demands, decrees, throne rooms and ideology role requirements.", 95,
    pats=["Title", "Decree", "IdeoRole", "IdeoLeader", "Throneroom", "Disinherited"],
    files=["RoyalTitles", "IdeoRole"])
cat(18, "ideology", "ideology beliefs", "Every thought a precept hands out, plus ideology buildings and raids. No preset touches these.", 92,
    pats=["IdeoBuilding", "ParticipatedInRaid", "RecentConquest", "FailedConvert"],
    files=["Precepts_", "IdeoBuilding", "IdeoRaiding"],
    precepts=True)
cat(19, "other", "everything else", "Thoughts no other category claimed. Mods' thoughts often land here.", 0,
    fallback=True)


def classify(defname, file, is_precept, traits, social):
    byp = sorted(C, key=lambda c: -c["prio"])
    for c in byp:
        if defname in c["names"]:
            return c["id"]
    if is_precept:
        for c in byp:
            if c["precepts"]:
                return c["id"]
    for text, key in ((defname, "pats"), (file, "files")):
        for c in byp:
            for p in c[key]:
                if p.lower() in text.lower():
                    return c["id"]
    if traits:
        for c in byp:
            if c["traits"]:
                return c["id"]
    if social:
        for c in byp:
            if c["social"]:
                return c["id"]
    return "other"


def simulate(tsv):
    out = {}
    with open(tsv) as f:
        for row in csv.reader(f, delimiter="\t"):
            pack, file, defname, tclass, worker, dur, traits = row[:7]
            is_precept = file.startswith("Precepts_") or "Precept" in worker
            social = "Social" in tclass or "Social" in worker
            out.setdefault(classify(defname, file, is_precept, traits != "", social), []).append(defname)
    for c in C:
        names = out.get(c["id"], [])
        print(f"\n## {c['id']} ({len(names)})")
        if c["id"] != "ideology":
            print(" ".join(sorted(names)))


def li(tag, values):
    if not values:
        return ""
    items = "".join(f"\n      <li>{escape(v)}</li>" for v in values)
    return f"\n    <{tag}>{items}\n    </{tag}>"


def write_xml(path):
    parts = ['<?xml version="1.0" encoding="utf-8"?>',
             "<!-- Thought categories. Other mods can add a ThoughtCategoryDef or patch a thought name into a category's thoughts list. -->",
             "<!-- Matching order: thoughts list, precept, defNameContains, fileNameContains, trait-gated, social, fallback. Higher priority wins a pattern tie. -->",
             "<Defs>"]
    for c in C:
        flags = "".join(f"\n    <{k}>true</{k}>" for k, v in (("matchPrecepts", c["precepts"]), ("matchTraitGated", c["traits"]), ("matchSocial", c["social"]), ("isFallback", c["fallback"])) if v)
        parts.append(f"""
  <ReasonableMoodsAndNeeds.Game.ThoughtCategoryDef>
    <defName>{c['id']}</defName>
    <label>{escape(c['label'])}</label>
    <description>{escape(c['desc'])}</description>
    <order>{c['order']}</order>
    <priority>{c['prio']}</priority>{li('thoughts', c['names'])}{li('defNameContains', c['pats'])}{li('fileNameContains', c['files'])}{flags}
  </ReasonableMoodsAndNeeds.Game.ThoughtCategoryDef>""")
    parts.append("\n</Defs>\n")
    with open(path, "w") as f:
        f.write("\n".join(parts[:4]) + "".join(parts[4:]))


if __name__ == "__main__":
    if len(sys.argv) != 3 or sys.argv[1] not in ("simulate", "write"):
        sys.exit(__doc__)
    if sys.argv[1] == "write":
        write_xml(sys.argv[2])
    else:
        simulate(sys.argv[2])
