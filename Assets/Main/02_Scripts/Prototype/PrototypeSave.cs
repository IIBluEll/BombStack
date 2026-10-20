using System;
using System.IO;
using BombStack.Data;
using UnityEngine;

namespace BombStack.Prototype
{
    internal static class PrototypeSave
    {
        private static string SavePath => Path.Combine(Application.persistentDataPath, "bomb-stack-prototype-v1.json");

        public static PersistentData Load(out string notice)
        {
            notice = "";
            try
            {
                if (!File.Exists(SavePath)) return new PersistentData();
                PersistentData data = JsonUtility.FromJson<PersistentData>(File.ReadAllText(SavePath));
                if (data == null || data.Version != 1 || data.Settings == null) throw new InvalidDataException("지원하지 않는 저장 형식입니다.");
                return data;
            }
            catch (Exception exception) when (exception is IOException || exception is UnauthorizedAccessException || exception is ArgumentException)
            {
                notice = "저장 데이터를 읽지 못해 기본값으로 시작합니다.";
                Debug.LogWarning(notice + " " + exception.Message);
                return new PersistentData();
            }
        }

        public static bool Save(PersistentData data, out string notice)
        {
            notice = "";
            try
            {
                Directory.CreateDirectory(Application.persistentDataPath);
                string temporary = SavePath + ".tmp";
                File.WriteAllText(temporary, JsonUtility.ToJson(data, true));
                if (File.Exists(SavePath)) File.Replace(temporary, SavePath, null);
                else File.Move(temporary, SavePath);
                return true;
            }
            catch (Exception exception) when (exception is IOException || exception is UnauthorizedAccessException || exception is ArgumentException)
            {
                notice = "기록 저장에 실패했습니다. 플레이는 계속할 수 있습니다.";
                Debug.LogWarning(notice + " " + exception.Message);
                return false;
            }
        }
    }
}
