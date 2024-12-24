using Newtonsoft.Json;
using ScanTextImage.Interface;
using ScanTextImage.Model;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace ScanTextImage.Service
{
    public class SaveDataService : ISaveDataService
    {
        public SaveModel SaveDataToFile(SaveModel saveModel)
        {
            var path = ConstData.Const.pathSaveData;
            if (!Directory.Exists(path))
            {
                // create data folder if not exist
                Directory.CreateDirectory(path);

                // set the id of file always equal 1
                saveModel.id = 1;
            }
            else
            {
                // if not have save id -> crete new
                if (!saveModel.id.HasValue)
                {
                    // get list data files
                    var listDataFile = Directory.GetFiles(path, "*.json")
                        .Select(Path.GetFileNameWithoutExtension)
                        .Order().ToList();

                    if (listDataFile.Count() >= 9) throw new Exception("Max save file is 9");

                    saveModel.id = listDataFile.Count + 1;
                }

                if (saveModel.nameSave.Length > 200)
                {
                    throw new Exception("Name save should be less than 200 characters");
                }

                if (string.IsNullOrWhiteSpace(saveModel.nameSave))
                {
                    saveModel.nameSave = "No Name " + saveModel.id.Value;
                }
            }

            var fileName = $"data_{saveModel.id}.json";
            string pathFile = Path.Combine(path, fileName);
            string jsonData = JsonConvert.SerializeObject(saveModel);


            using (var file = new FileStream(pathFile, FileMode.Create))
            {
                using (var sw = new StreamWriter(file))
                {
                    Debug.WriteLine("Save file data - " + saveModel.id);
                    sw.WriteLine(jsonData);
                }
            }

            return saveModel;
        }

        public List<SaveModel> GetListSaveData()
        {
            var path = ConstData.Const.pathSaveData;
            if (!Directory.Exists(path))
            {
                return new List<SaveModel>();
            }

            // get list data files
            var listDataFile = Directory.GetFiles(path, "*.json")
                .Order().ToList();

            var listData = new List<SaveModel>();


            foreach (var item in listDataFile)
            {
                string json = File.ReadAllText(item);
                SaveModel data = JsonConvert.DeserializeObject<SaveModel>(json);
                listData.Add(data);
            }

            return listData.OrderBy(x => x.id).ToList();
        }

        public void DeleteDataFile(int? id)
        {
            var path = ConstData.Const.pathSaveData;

            if (!id.HasValue || id.Value < 0 || id.Value > 9)
            {
                throw new Exception("Invalid Id data");
            }

            if (!Directory.Exists(path))
            {
                throw new Exception("Not exist");
            }

            var filePath = Directory.GetFiles(path, "*.json")
                .FirstOrDefault(name =>
                {
                    var match = Regex.Match(name, ConstData.Const.regexSaveFileName, RegexOptions.IgnoreCase, TimeSpan.FromSeconds(5));
                    var idFile = Convert.ToInt32(match.Groups["idSave"].Value);
                    return !string.IsNullOrWhiteSpace(name) && idFile == id.Value;
                }) ?? throw new Exception("Not exist");

            File.Delete(filePath);

        }

        public List<ShortcutModel> SaveShortcut(List<ShortcutModel> saveShortcuts)
        {
            var path = ConstData.Const.pathConfigData;

            var invalidUpdate = saveShortcuts.Where(data => (!data.IsAltKey && !data.IsShiftKey && !data.IsControlKey) || string.IsNullOrEmpty(data.Key)).ToList();
            var duplicateUpdate = saveShortcuts.Select(data => (data.IsControlKey, data.IsShiftKey, data.IsAltKey, data.Key)).Distinct().ToList();

            // if not have modified or key => error
            if (invalidUpdate.Count() > 0)
            {
                throw new Exception("Invalid update shortcut");
            }

            if (duplicateUpdate.Count() != saveShortcuts.Count)
            {
                throw new Exception("Duplicate shortcut");
            }

            string jsonData = JsonConvert.SerializeObject(saveShortcuts);

            using (var file = new FileStream(path, FileMode.Create))
            {
                using (var sw = new StreamWriter(file))
                {
                    sw.WriteLine(jsonData);
                }
            }

            return saveShortcuts;
        }

        public List<ShortcutModel> GetShortcutConfig()
        {
            var path = ConstData.Const.pathConfigData;
            if (!File.Exists(path))
            {
                return new List<ShortcutModel>();
            }

            // get list data files

            var listData = new List<ShortcutModel>();
            string json = File.ReadAllText(path);
            listData = JsonConvert.DeserializeObject<List<ShortcutModel>>(json);


            return listData;
        }
    }
}
