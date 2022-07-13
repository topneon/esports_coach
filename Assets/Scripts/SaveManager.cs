using UnityEngine;
using System.IO;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters.Binary;

public static class SaveManager
{
    public static string directory = "esportm", save = "save1";

    public static void Save<T>(T stat, string fileName)
    {
        using (FileStream file = new FileStream(GetFullPath(fileName), FileMode.Create))
        {
            BinaryFormatter bf = new BinaryFormatter();
            bf.Serialize(file, stat);
        }
    }

    public static T Load<T>(string fileName)
    {
        using (var stream = File.Open(GetFullPath(fileName), FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.ReadWrite))
        {
            BinaryFormatter bf = new BinaryFormatter();
            T stat = (T)bf.Deserialize(stream);
            return stat;
        }
    }

    public static void SaveSetting<T>(T stat, string fileName)
    {
        using (FileStream file = new FileStream(GetFullPathSetting(fileName), FileMode.Create))
        {
            BinaryFormatter bf = new BinaryFormatter();
            bf.Serialize(file, stat);
        }
    }

    public static T LoadSetting<T>(string fileName)
    {
        using (var stream = File.Open(GetFullPathSetting(fileName), FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.ReadWrite))
        {
            BinaryFormatter bf = new BinaryFormatter();
            T stat = (T)bf.Deserialize(stream);
            return stat;
        }
    }

    public static void DeleteFile(string fileName)
    {
        File.Delete(GetFullPath(fileName));
    }

    public static void DeleteFileSetting(string fileName)
    {
        File.Delete(GetFullPathSetting(fileName));
    }

    public static void DeleteAll()
    {
        DirectoryInfo _directory = new DirectoryInfo(Application.persistentDataPath + "/" + directory + "/" + save);
        foreach (FileInfo file in _directory.GetFiles()) file.Delete();
        foreach (DirectoryInfo subDirectory in _directory.GetDirectories()) subDirectory.Delete(true);
    }

    public static bool SaveExists(string fileName)
    {
        return File.Exists(GetFullPath(fileName));
    }

    public static bool SaveExistsSetting(string fileName)
    {
        return File.Exists(GetFullPathSetting(fileName));
    }

    public static void DirectoryExistsOrCreate()
    {
        if (!Directory.Exists(Application.persistentDataPath + "/" + directory))
            Directory.CreateDirectory(Application.persistentDataPath + "/" + directory);
        if (!Directory.Exists(Application.persistentDataPath + "/" + directory + "/save1"))
            Directory.CreateDirectory(Application.persistentDataPath + "/" + directory + "/save1");
        if (!Directory.Exists(Application.persistentDataPath + "/" + directory + "/save2"))
            Directory.CreateDirectory(Application.persistentDataPath + "/" + directory + "/save2");
        if (!Directory.Exists(Application.persistentDataPath + "/" + directory + "/save3"))
            Directory.CreateDirectory(Application.persistentDataPath + "/" + directory + "/save3");
        if (!Directory.Exists(Application.persistentDataPath + "/" + directory + "/save4"))
            Directory.CreateDirectory(Application.persistentDataPath + "/" + directory + "/save4");
        if (!Directory.Exists(Application.persistentDataPath + "/" + directory + "/save5"))
            Directory.CreateDirectory(Application.persistentDataPath + "/" + directory + "/save5");
        if (!Directory.Exists(Application.persistentDataPath + "/Mods"))
            Directory.CreateDirectory(Application.persistentDataPath + "/Mods");
    }

    private static string GetFullPath(string fileName)
    {
        return Application.persistentDataPath + "/" + directory + "/" + save + "/" + fileName + ".bin";
    }

    private static string GetFullPathSetting(string fileName)
    {
        return Application.persistentDataPath + "/" + directory + "/" + fileName + ".bin";
    }
}
