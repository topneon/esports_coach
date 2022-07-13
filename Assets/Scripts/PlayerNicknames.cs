using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerNicknames : MonoBehaviour
{
    public static string[] nickname = new string[]
    {
        "Croctucky", "CrocnBalls", "bennyS", "fleXi", "Billy Herrington", "Joker", "Blownwowzk1",
        "NeyKING", "Unclepaje", "Mikey", "Codigo", "Chunky", "Hater", "Speen_", "Fallcool",
        "tom4712", "Deichmett", "LetsTube", "sk", "Frank", "Aicu_45", "Kikay", "Bugs Bunny", "Kito",
        "SHADOU", "GOTDAMN", "stalker1298", "Fristajlo", "Levofuc", "Splinter", "prokda", "mazu-",
        "ache.", "m1tur.", "Oktay", "rolandoWK", "Darcy", "masterbonsai", "Stone", "MFXS", "junkie",
        "Old G", "MustafaYTV", "Gazolin3", "Lukaaaa1", "kseovir", "Hasimee", "kktz3rkk", "f0wz",
        "Hozzy_X", "Maxim_MPD", "Klivverr", "dexyy", "Obla", "FroZken", "heartbrok3n", "zaksty", "Obla",
        "zxcdead", "glad", "b0t-", "whysamurai", "justCause", "expronyx", "viewmodel", "kfg", 
        "STRETCHED", "fesii", "ra1", "Acoll", "AciRai", "fuRllyS", "Malbert", "AWAJA", "xaris0",
        "bazar543", "BeNeFiT", "THunR", "Rave", "damager", "CoVeRBanG", "m535i", "Lowe", "Nevrox",
        "Nysson", "SLiseR", "shapi", "Ruudi", "gambl3r", "prep", "N0Name", "jand", "crebel",
        "arsebe", "yiotro", "Sojo", "Kamizera", "Bolonha", "vaiber", "Mark", "Fistasz", "reedlock",
        "M4TEWS", "S1lly", "froelz", "Veight", "ASUKA", "Sweet", "xencocks", "Outcast", "Malkiss",
        "Crayshy", "twisther", "Michael974", "Sh4duw", "Vol4ik", "bebr228", "luffy77", "Vedoth",
        "rycienagegre", "vxli", "LovelierDog", "JokeD", "vlapommy", "aperturien", "Levap Habla",
        "ERLAK", "Geem", "Queroz", "BBDS", "Derx", "Dank", "Heop", "Iraq", "Kostek", "AnD",
        "elgn1s", "Scrim", "Niko7o", "klur1n1", "Mr_Aero", "-Zedo", "R1mens", "yasteap", "Swizzzy",
        "KIBORG", "Startym", "agacer-ot", "Knaert", "Hoovle", "EmeZe", "KINDER", "GlennTonic",
        "Mattey17", "-tmn", "n1coo-", "pxulaner", "Potky", "AakeT", "Ratta-", "PuQi", "JihanX", "at0m",
        "n4k4s1", "Ram1s", "matros", "Grenlain", "ShodiXxX", "zUchiha", "art1k", "czaki", "bag1ra4k",
        "CookTime", "Monty9", "IForyk", "sivid", "zigurat", "Lemyy", "tab1", "Hard_sk", "BatmonR",
        "Amoxap1ne"
    };

    public static List<string> listsos = new List<string>();

    public static void Add(string text)
    {
        listsos.Add(text);
    }
    public List<string> _nicknames;
    [ContextMenu("GOVNO")]
    public void End() { _nicknames.AddRange(listsos); }

    public static Player GeneratePlayerLeague()
    {
        Player p = new Player();
        byte strength = (byte)Random.Range(55, 74);
        p.GeneratePlayer(
            nickname[Random.Range(0, nickname.Length)],
            (Manager.Role)Random.Range(0, 5),
            (Manager.Activity)Random.Range(0, 3),
            (Manager.Nation)Random.Range(0, Manager.mainInstance.GetNations().Length),
            strength,
            true,
            false,
            true);
        return p;
    }

    public static Player GeneratePlayerLeague(Manager.Role role)
    {
        Player p = new Player();
        byte strength = (byte)Random.Range(55, 95);
        p.GeneratePlayer(
            nickname[Random.Range(0, nickname.Length)],
            role,
            (Manager.Activity)Random.Range(0, 3),
            (Manager.Nation)Random.Range(0, Manager.mainInstance.GetNations().Length),
            strength,
            true,
            false,
            true);
        return p;
    }

    public static Player GeneratePlayerLeague(Manager.Role role, string teamname)
    {
        Player p = new Player();
        p.teamName = teamname;
        byte strength = (byte)Random.Range(55, 95);
        p.GeneratePlayer(
            nickname[Random.Range(0, nickname.Length)],
            role,
            (Manager.Activity)Random.Range(0, 3),
            (Manager.Nation)Random.Range(0, Manager.mainInstance.GetNations().Length),
            strength,
            false,
            false,
            true);
        return p;
    }


    public static Player GeneratePlayerAcademy(byte level, Manager.Role role)
    {
        Player p = new Player();
        byte strength = (byte)(Random.Range(-5, 10) + level + 55);
        p.GeneratePlayer(
            nickname[Random.Range(0, nickname.Length)],
            role,
            (Manager.Activity)Random.Range(0, 3),
            (Manager.Nation)Random.Range(0, Manager.mainInstance.GetNations().Length),
            strength,
            true,
            false,
            true);
        return p;
    }

    public static Player GeneratePlayerAcademy(byte level, Manager.Role role, string teamname)
    {
        Player p = new Player();
        p.teamName = teamname;
        byte strength = (byte)(Random.Range(-5, 10) + level + 55);
        p.GeneratePlayer(
            nickname[Random.Range(0, nickname.Length)],
            role,
            (Manager.Activity)Random.Range(0, 3),
            (Manager.Nation)Random.Range(0, Manager.mainInstance.GetNations().Length),
            strength,
            false,
            false,
            true);
        return p;
    }
}
