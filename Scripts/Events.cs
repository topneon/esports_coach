using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Events : MonoBehaviour
{
    public static Events events;
    private void Start()
    {
        events = this;
    }

    public void SetTournaments(List<Tournament> t) { tournamentList1 = t; }
    public void AddTournaments(List<Tournament> t) { tournamentList1.AddRange(t); }

    public List<Tournament> GetTournaments()
    {
        return tournamentList1;
    }

    public int GetTournamentIndex(Tournament tournament)
    {
        for (short i = 0; i < tournamentList1.Count; i++)
            if (tournament.day == tournamentList1[i].day) return i;
        return -1;
    }

    string[] title = new string[] 
    { "EXTM", "StarSeries", "EPL", "Hypecenter", "BOOM Premier", "DH Open", "DH Masters" };
    string[] logo = new string[] { "iem", "star", "esl", "hype", "blast", "dh", "dh" };
    string[] type = new string[] { "Major", "T1 LAN", "T2 LAN", "Online" };
    string[] cities = new string[]
    {
        "Sevilla", "Sydney", "Bayern", "Kiyv", "Malmo", "Leipzig", "Dallas", "Moscow", "Katowice",
        "Cologne", "Bejing", "Stockholm", "Copenhagen", "Budapest", "Berlin", "Boston", "New York",
        "London", "Helsinki", "Dubai", "Malta", "Odense", "Shanghai", "Atlanta", "Krakow", "Prague",
        "Rotterdam", "Chicago", "Valencia", "Montpellier", "Paris", "Lisbon", "Leicester", "Tours",
        "Madrid", "Barcelona", "Miami", "Warsaw", "Jonkoping", "Istanbul", "Montreal", "Amriswil",
        "Hong Kong", "Belo Horizonte", "Rio", "Marseille", "Belgrade", "Minsk", "Cancun", "Toronto",
        "Bucharest", "Seoul", "Hamburg", "Las Vegas", "Sao Paulo", "Jakarta", "Columbus", "Split",
        "Vilnius", "Birmingham", "Milan", "Vienna", "Singapore", "Kemer", "Melbourne"
    };
    short[] prizePools = new short[] { 2000, 800, 100, 25 };

    public void GenerateTournaments(byte years = 10)
    {
        //tier 3 is 25k tier 2 is 100k tier 1 is 800k major is 2000k
        // month - tiers(m - major, q - quals needed, c - only closed, p - points for major) (count)
        // 0 - (3q, 3, 2q) (5)
        // 1 - (3, 2q, 1qp) (5)
        // 2 - (1c, 1q, 2) (4)
        // 3 - (2q, 3, 1qp) (5)
        // 4 - (1q, 0qp, 1c) (5)
        // 5 - (1cp, 0p, 0p, 0mp) (4)
        // 6 - (3, 3q, 1q) (5)
        // 7 - (1qp, 1c, 2q) (5)
        // 8 - (1q, 1c, 2q) (5)
        // 9 - (2, 2q, 0qp) (5)
        // 10 - (1cp, 0p, 0p, 0mp) (4)
        // 11 - (1q, 1q, 3) (5)
        // 38 events a year (0 - x8, 1 - x15, 2 - x8, 3 - x7)
        // 19 qualifiers a year (0 - x2, 1 - x10, 2 - x5, 3 - x3)
        // NEW
        // 18% tier 3
        // 27% tier 2
        // 55% tier 1
        // major months forced only
        tournamentList1 = new List<Tournament>();
        int day = 2, iT = 0;
        for (byte i = 0; i < years; i++)
        {
            List<Tournament> tournaments = new List<Tournament>(16);
            Tournament[] ts = new Tournament[57];
            byte majorMonth = (byte)Random.Range(0, 6);
            for (byte j = 0; j < 12; j++)
            {
                day = i * 360 + j * 30 + 2;
                byte daysToSpare = 28;
                int calls = 0;
                while (daysToSpare > 3 && calls < 15)
                {
                    Tournament tfinal, tqual;
                    if (majorMonth == j % 6)
                    {
                        while (daysToSpare > 14)
                        {
                            Tier0P(day + (28 - daysToSpare), out tfinal);
                            daysToSpare -= (byte)Modding.modding.modSave.structure[tfinal.btype].dayLength;
                            tournaments.Add(tfinal);
                            iT++;
                        }
                        bool ll = false;
                        while (!ll)
                        {
                            Tier0MP(day + (28 - daysToSpare), out tfinal);
                            if (daysToSpare >= (byte)Modding.modding.modSave.structure[tfinal.btype].dayLength)
                            {
                                ll = true;
                                daysToSpare -= (byte)Modding.modding.modSave.structure[tfinal.btype].dayLength;
                                tournaments.Add(tfinal);
                                iT++;
                            }
                        }
                        break;
                    }
                    else
                    {
                        calls++;
                        byte random1 = (byte)Random.Range(0, 100), random2;
                        if (random1 < 18) // tier 3
                        {
                            random2 = (byte)Random.Range(0, 100);
                            if (random2 < 50)
                            {
                                Tier3(day + (28 - daysToSpare), out tfinal);
                                if (daysToSpare >= (byte)Modding.modding.modSave.structure[tfinal.btype].dayLength)
                                {
                                    daysToSpare -= (byte)Modding.modding.modSave.structure[tfinal.btype].dayLength;
                                    tournaments.Add(tfinal);
                                    iT++;
                                }
                                continue;
                            }
                            else
                            {
                                Tier3Q(in iT, day + (28 - daysToSpare), out tqual, out tfinal);
                                if (daysToSpare >= 
                                    Modding.modding.modSave.structure[tfinal.btype].dayLength +
                                    Modding.modding.modSave.structure[tqual.btype].dayLength)
                                {
                                    daysToSpare -= (byte)(Modding.modding.modSave.structure[tfinal.btype].dayLength +
                                    Modding.modding.modSave.structure[tqual.btype].dayLength);
                                    tournaments.Add(tqual);
                                    tournaments.Add(tfinal);
                                    iT += 2;
                                }
                                continue;
                            }
                        }
                        else if (random1 < 45) // tier 2
                        {
                            random2 = (byte)Random.Range(0, 100);
                            if (random2 < 50)
                            {
                                Tier2(day + (28 - daysToSpare), out tfinal);
                                if (daysToSpare >= (byte)Modding.modding.modSave.structure[tfinal.btype].dayLength)
                                {
                                    daysToSpare -= (byte)Modding.modding.modSave.structure[tfinal.btype].dayLength;
                                    tournaments.Add(tfinal);
                                    iT++;
                                }
                                continue;
                            }
                            else
                            {
                                Tier2Q(in iT, day + (28 - daysToSpare), out tqual, out tfinal);
                                if (daysToSpare >=
                                    Modding.modding.modSave.structure[tfinal.btype].dayLength +
                                    Modding.modding.modSave.structure[tqual.btype].dayLength)
                                {
                                    daysToSpare -= (byte)(Modding.modding.modSave.structure[tfinal.btype].dayLength +
                                    Modding.modding.modSave.structure[tqual.btype].dayLength);
                                    tournaments.Add(tqual);
                                    tournaments.Add(tfinal);
                                    iT += 2;
                                }
                                continue;
                            }
                        }
                        else // tier 1
                        {
                            random2 = (byte)Random.Range(0, 100);
                            if (random2 < 32)
                            {
                                Tier1C(day + (28 - daysToSpare), out tfinal);
                                if (daysToSpare >= (byte)Modding.modding.modSave.structure[tfinal.btype].dayLength)
                                {
                                    daysToSpare -= (byte)Modding.modding.modSave.structure[tfinal.btype].dayLength;
                                    tournaments.Add(tfinal);
                                    iT++;
                                }
                                continue;
                            }
                            else if (random2 < 50)
                            {
                                Tier1CP(day + (28 - daysToSpare), out tfinal);
                                if (daysToSpare >= (byte)Modding.modding.modSave.structure[tfinal.btype].dayLength)
                                {
                                    daysToSpare -= (byte)Modding.modding.modSave.structure[tfinal.btype].dayLength;
                                    tournaments.Add(tfinal);
                                    iT++;
                                }
                                continue;
                            }
                            else if (random2 < 82)
                            {
                                Tier1Q(in iT, day + (28 - daysToSpare), out tqual, out tfinal);
                                if (daysToSpare >=
                                    Modding.modding.modSave.structure[tfinal.btype].dayLength +
                                    Modding.modding.modSave.structure[tqual.btype].dayLength)
                                {
                                    daysToSpare -= (byte)(Modding.modding.modSave.structure[tfinal.btype].dayLength +
                                    Modding.modding.modSave.structure[tqual.btype].dayLength);
                                    tournaments.Add(tqual);
                                    tournaments.Add(tfinal);
                                    iT += 2;
                                }
                                continue;
                            }
                            else
                            {
                                Tier1QP(in iT, day + (28 - daysToSpare), out tqual, out tfinal);
                                if (daysToSpare >=
                                    Modding.modding.modSave.structure[tfinal.btype].dayLength +
                                    Modding.modding.modSave.structure[tqual.btype].dayLength)
                                {
                                    daysToSpare -= (byte)(Modding.modding.modSave.structure[tfinal.btype].dayLength +
                                    Modding.modding.modSave.structure[tqual.btype].dayLength);
                                    tournaments.Add(tqual);
                                    tournaments.Add(tfinal);
                                    iT += 2;
                                }
                                continue;
                            }
                        }
                    }
                }
            }
            tournamentList1.AddRange(tournaments);
            /*
            // 0
            Tier3Q((i * 57), (i * 360) + 2, out ts[0], out ts[1]);
            Tier3((i * 360) + 12, out ts[2]);
            Tier2Q((i * 57) + 3, (i * 360) + 19, out ts[3], out ts[4]);
            // 1
            Tier3((i * 360) + 32, out ts[5]);
            Tier2Q((i * 57) + 6, (i * 360) + 39, out ts[6], out ts[7]);
            Tier1QP((i * 57) + 8, (i * 360) + 49, out ts[8], out ts[9]);
            // 2
            Tier1C((i * 360) + 62, out ts[10]);
            Tier1Q((i * 57) + 11, (i * 360) + 69, out ts[11], out ts[12]);
            Tier2((i * 360) + 79, out ts[13]);
            // 3
            Tier2Q((i * 57) + 14, (i * 360) + 92, out ts[14], out ts[15]);
            Tier3((i * 360) + 102, out ts[16]);
            Tier1QP((i * 57) + 17, (i * 360) + 109, out ts[17], out ts[18]);
            // 4
            Tier1Q((i * 57) + 19, (i * 360) + 122, out ts[19], out ts[20]);
            Tier0QP((i * 57) + 21, (i * 360) + 132, out ts[21], out ts[22]);
            Tier1C((i * 360) + 142, out ts[23]);
            // 5
            Tier1CP((i * 360) + 152, out ts[24]);
            Tier0P((i * 360) + 159, out ts[25]);
            Tier0P((i * 360) + 166, out ts[26]);
            Tier0MP((i * 360) + 173, out ts[27]);
            // 6
            Tier3Q((i * 57) + 28, (i * 360) + 182, out ts[28], out ts[29]);
            Tier3((i * 360) + 192, out ts[30]);
            Tier1Q((i * 57) + 31, (i * 360) + 199, out ts[31], out ts[32]);
            // 7
            Tier1QP((i * 57) + 33, (i * 360) + 212, out ts[33], out ts[34]);
            Tier1C((i * 360) + 222, out ts[35]);
            Tier2Q((i * 57) + 36, (i * 360) + 229, out ts[36], out ts[37]);
            // 8
            Tier1Q((i * 57) + 38, (i * 360) + 242, out ts[38], out ts[39]);
            Tier1C((i * 360) + 252, out ts[40]);
            Tier2Q((i * 57) + 41, (i * 360) + 259, out ts[41], out ts[42]);
            // 9
            Tier2Q((i * 57) + 43, (i * 360) + 272, out ts[43], out ts[44]);
            Tier2((i * 360) + 282, out ts[45]);
            Tier0QP((i * 57) + 46, (i * 360) + 289, out ts[46], out ts[47]);
            // 10
            Tier1CP((i * 360) + 302, out ts[48]);
            Tier0P((i * 360) + 309, out ts[49]);
            Tier0P((i * 360) + 316, out ts[50]);
            Tier0MP((i * 360) + 323, out ts[51]);
            // 11
            Tier1Q((i * 57) + 52, (i * 360) + 332, out ts[52], out ts[53]);
            Tier1Q((i * 57) + 54, (i * 360) + 342, out ts[54], out ts[55]);
            Tier3((i * 360) + 352, out ts[56]);*/
            //tournamentList1.AddRange(ts);
        }
    }

    private void Tier3Q(in int indexer, in int day, out Tournament qual, out Tournament tournament)
    {
        int l = Random.Range(0, title.Length), r = Random.Range(0, cities.Length);
        string tt = title[l] + " " + cities[r];
        string tq = title[l] + " (Qual)";
        byte randomQual;
        do { randomQual = (byte)Random.Range(0, Modding.modding.modSave.structure.Length); }
        while (Modding.modding.modSave.structure[randomQual].stage != "group");
        qual = new Tournament(
            tq, // title
            type[3], // type
            3, // tier
            (prizePools[3] + Random.Range(-15, 26)) * 200, // prizepool
            day, // day
            randomQual, // btype
            Manager.mainInstance.LogoSprite(logo[l]), // logo
            false, // isMajor
            0, // majorPoints
            1, // open
            indexer + 1); // unlockEvent
        byte randomFinal;
        do { randomFinal = (byte)Random.Range(0, Modding.modding.modSave.structure.Length); }
        while (Modding.modding.modSave.structure[randomFinal].stage != "final" ||
        Modding.modding.modSave.structure[randomFinal].teamPool >=
        Modding.modding.modSave.structure[randomQual].teamPool);
        tournament = new Tournament(
            tt, // title
            type[3], // type
            3, // tier
            (prizePools[3] + Random.Range(-15, 26)) * 1000, // prizepool
            day + 3, // day
            randomFinal, // btype
            Manager.mainInstance.LogoSprite(logo[l]), // logo
            false, // isMajor
            0, // majorPoints
            0, // open
            -1); // unlockEvent
    }

    private void Tier3(in int day, out Tournament tournament)
    {
        int l = Random.Range(0, title.Length), r = Random.Range(0, cities.Length);
        string tt = title[l] + " " + cities[r];
        byte randomFinal;
        do { randomFinal = (byte)Random.Range(0, Modding.modding.modSave.structure.Length); }
        while (Modding.modding.modSave.structure[randomFinal].stage != "final");
        tournament = new Tournament(
            tt, // title
            type[3], // type
            3, // tier
            (prizePools[3] + Random.Range(-15, 26)) * 1000, // prizepool
            day, // day
            randomFinal, // btype
            Manager.mainInstance.LogoSprite(logo[l]), // logo
            false, // isMajor
            0, // majorPoints
            1, // open
            -1); // unlockEvent
    }

    private void Tier2Q(in int indexer, in int day, out Tournament qual, out Tournament tournament)
    {
        int l = Random.Range(0, title.Length), r = Random.Range(0, cities.Length);
        string tt = title[l] + " " + cities[r];
        string tq = title[l] + " (Qual)";
        byte randomQual;
        do { randomQual = (byte)Random.Range(0, Modding.modding.modSave.structure.Length); }
        while (Modding.modding.modSave.structure[randomQual].stage != "group");
        qual = new Tournament(
            tq, // title
            type[2], // type
            2, // tier
            (prizePools[2] + Random.Range(-50, 51)) * 400, // prizepool
            day, // day
            randomQual, // btype
            Manager.mainInstance.LogoSprite(logo[l]), // logo
            false, // isMajor
            0, // majorPoints
            1, // open
            indexer + 1); // unlockEvent
        byte randomFinal;
        do { randomFinal = (byte)Random.Range(0, Modding.modding.modSave.structure.Length); }
        while (Modding.modding.modSave.structure[randomFinal].stage != "final" ||
        Modding.modding.modSave.structure[randomFinal].teamPool >=
        Modding.modding.modSave.structure[randomQual].teamPool);
        tournament = new Tournament(
            tt, // title
            type[2], // type
            2, // tier
            (prizePools[2] + Random.Range(-50, 101)) * 1000, // prizepool
            day + 3, // day
            randomFinal, // btype
            Manager.mainInstance.LogoSprite(logo[l]), // logo
            false, // isMajor
            0, // majorPoints
            0, // open
            -1); // unlockEvent
    }

    private void Tier2(in int day, out Tournament tournament)
    {
        int l = Random.Range(0, title.Length), r = Random.Range(0, cities.Length);
        string tt = title[l] + " " + cities[r];
        byte randomFinal;
        do { randomFinal = (byte)Random.Range(0, Modding.modding.modSave.structure.Length); }
        while (Modding.modding.modSave.structure[randomFinal].stage != "final");
        tournament = new Tournament(
            tt, // title
            type[2], // type
            2, // tier
            (prizePools[2] + Random.Range(-50, 101)) * 1000, // prizepool
            day, // day
            randomFinal, // btype
            Manager.mainInstance.LogoSprite(logo[l]), // logo
            false, // isMajor
            0, // majorPoints
            1, // open
            -1); // unlockEvent
    }

    private void Tier1Q(in int indexer, in int day, out Tournament qual, out Tournament tournament)
    {
        int l = Random.Range(0, title.Length), r = Random.Range(0, cities.Length);
        string tt = title[l] + " " + cities[r];
        string tq = title[l] + " (Qual)";
        byte randomQual;
        do { randomQual = (byte)Random.Range(0, Modding.modding.modSave.structure.Length); }
        while (Modding.modding.modSave.structure[randomQual].stage != "group");
        qual = new Tournament(
            tq, // title
            type[1], // type
            1, // tier
            (prizePools[1] + Random.Range(-300, 451)) * 200, // prizepool
            day, // day
            randomQual, // btype
            Manager.mainInstance.LogoSprite(logo[l]), // logo
            false, // isMajor
            0, // majorPoints
            1, // open
            indexer + 1); // unlockEvent
        byte randomFinal;
        do { randomFinal = (byte)Random.Range(0, Modding.modding.modSave.structure.Length); }
        while (Modding.modding.modSave.structure[randomFinal].stage != "final" ||
        Modding.modding.modSave.structure[randomFinal].teamPool >=
        Modding.modding.modSave.structure[randomQual].teamPool);
        tournament = new Tournament(
            tt, // title
            type[1], // type
            1, // tier
            (prizePools[1] + Random.Range(-400, 501)) * 1000, // prizepool
            day + 3, // day
            randomFinal, // btype
            Manager.mainInstance.LogoSprite(logo[l]), // logo
            false, // isMajor
            0, // majorPoints
            0, // open
            -1); // unlockEvent
    }

    private void Tier1C(in int day, out Tournament tournament)
    {
        int l = Random.Range(0, title.Length), r = Random.Range(0, cities.Length);
        string tt = title[l] + " " + cities[r];
        byte randomFinal;
        do { randomFinal = (byte)Random.Range(0, Modding.modding.modSave.structure.Length); }
        while (Modding.modding.modSave.structure[randomFinal].stage != "final");
        tournament = new Tournament(
            tt, // title
            type[1], // type
            1, // tier
            (prizePools[1] + Random.Range(-400, 501)) * 1000, // prizepool
            day, // day
            randomFinal, // btype
            Manager.mainInstance.LogoSprite(logo[l]), // logo
            false, // isMajor
            0, // majorPoints
            0, // open
            -1); // unlockEvent
    }

    private void Tier1QP(in int indexer, in int day, out Tournament qual, out Tournament tournament)
    {
        int l = Random.Range(0, title.Length), r = Random.Range(0, cities.Length);
        string tt = title[l] + " " + cities[r];
        string tq = title[l] + " (Qual)";
        byte randomQual;
        do { randomQual = (byte)Random.Range(0, Modding.modding.modSave.structure.Length); }
        while (Modding.modding.modSave.structure[randomQual].stage != "group");
        qual = new Tournament(
            tq, // title
            type[1], // type
            1, // tier
            (prizePools[1] + Random.Range(-300, 451)) * 200, // prizepool
            day, // day
            randomQual, // btype
            Manager.mainInstance.LogoSprite(logo[l]), // logo
            false, // isMajor
            0, // majorPoints
            1, // open
            indexer + 1); // unlockEvent
        byte randomFinal;
        do { randomFinal = (byte)Random.Range(0, Modding.modding.modSave.structure.Length); }
        while (Modding.modding.modSave.structure[randomFinal].stage != "final" ||
        Modding.modding.modSave.structure[randomFinal].teamPool >=
        Modding.modding.modSave.structure[randomQual].teamPool);
        tournament = new Tournament(
            tt, // title
            type[1], // type
            1, // tier
            (prizePools[1] + Random.Range(-400, 501)) * 1000, // prizepool
            day + 3, // day
            randomFinal, // btype
            Manager.mainInstance.LogoSprite(logo[l]), // logo
            false, // isMajor
            (byte)(50 + Random.Range(0, 30)), // majorPoints
            0, // open
            -1); // unlockEvent
    }

    private void Tier1CP(in int day, out Tournament tournament)
    {
        int l = Random.Range(0, title.Length), r = Random.Range(0, cities.Length);
        string tt = title[l] + " " + cities[r];
        byte randomFinal;
        do { randomFinal = (byte)Random.Range(0, Modding.modding.modSave.structure.Length); }
        while (Modding.modding.modSave.structure[randomFinal].stage != "final");
        tournament = new Tournament(
            tt, // title
            type[1], // type
            1, // tier
            (prizePools[1] + Random.Range(-400, 501)) * 1000, // prizepool
            day, // day
            randomFinal, // btype
            Manager.mainInstance.LogoSprite(logo[l]), // logo
            false, // isMajor
            (byte)(50 + Random.Range(0, 30)), // majorPoints
            0, // open
            -1); // unlockEvent
    }

    private void Tier0QP(in int indexer, in int day, out Tournament qual, out Tournament tournament)
    {
        int l = Random.Range(0, title.Length), r = Random.Range(0, cities.Length);
        string tt = title[l] + " " + cities[r];
        string tq = title[l] + " (Qual)";
        byte randomQual;
        do { randomQual = (byte)Random.Range(0, Modding.modding.modSave.structure.Length); }
        while (Modding.modding.modSave.structure[randomQual].stage != "group");
        qual = new Tournament(
            tq, // title
            type[1], // type
            0, // tier
            (prizePools[1] + Random.Range(-300, 201)) * 200, // prizepool
            day, // day
            randomQual, // btype
            Manager.mainInstance.LogoSprite(logo[l]), // logo
            false, // isMajor
            0, // majorPoints
            1, // open
            indexer + 1); // unlockEvent
        byte randomFinal;
        do { randomFinal = (byte)Random.Range(0, Modding.modding.modSave.structure.Length); }
        while (Modding.modding.modSave.structure[randomFinal].stage != "final" ||
        Modding.modding.modSave.structure[randomFinal].teamPool >=
        Modding.modding.modSave.structure[randomQual].teamPool);
        tournament = new Tournament(
            tt, // title
            "Major Qual", // type
            0, // tier
            (prizePools[1] + Random.Range(-600, 501)) * 1000, // prizepool
            day + 3, // day
            randomFinal, // btype
            Manager.mainInstance.LogoSprite(logo[l]), // logo
            false, // isMajor
            (byte)(100 + Random.Range(0, 60)), // majorPoints
            0, // open
            -1); // unlockEvent
    }

    private void Tier0P(in int day, out Tournament tournament)
    {
        int l = Random.Range(0, title.Length), r = Random.Range(0, cities.Length);
        string tt = title[l] + " " + cities[r];
        byte randomFinal;
        do { randomFinal = (byte)Random.Range(0, Modding.modding.modSave.structure.Length); }
        while (Modding.modding.modSave.structure[randomFinal].stage != "final");
        tournament = new Tournament(
            tt, // title
            "Major Qual", // type
            0, // tier
            (prizePools[1] + Random.Range(-600, 501)) * 1000, // prizepool
            day, // day
            randomFinal, // btype
            Manager.mainInstance.LogoSprite(logo[l]), // logo
            false, // isMajor
            (byte)(100 + Random.Range(0, 60)), // majorPoints
            1, // open
            -1); // unlockEvent
    }

    private void Tier0MP(in int day, out Tournament tournament)
    {
        int l = Random.Range(0, title.Length), r = Random.Range(0, cities.Length);
        string tt = title[l] + " " + cities[r];
        byte randomFinal;
        do { randomFinal = (byte)Random.Range(0, Modding.modding.modSave.structure.Length); }
        while (Modding.modding.modSave.structure[randomFinal].stage != "final");
        tournament = new Tournament(
            tt, // title
            type[0], // type
            0, // tier
            (prizePools[0] + Random.Range(-500, 1001)) * 1000, // prizepool
            day, // day
            randomFinal, // btype
            Manager.mainInstance.LogoSprite(logo[l]), // logo
            true, // isMajor
            254, // majorPoints
            0, // open
            -1); // unlockEvent
    }

    [SerializeField] private List<Tournament> tournamentList1;
}
