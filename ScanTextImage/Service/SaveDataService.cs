using Newtonsoft.Json;
using ScanTextImage.Interface;
using ScanTextImage.Model;
using Serilog;
using System.IO;
using System.Text.RegularExpressions;
using System.Windows.Input;
using System.Windows.Media.Imaging;

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
            var listKeyPress = saveShortcuts.Select(data => data.Key).ToList();

            // if not have modified or key => error
            if (invalidUpdate.Count() > 0)
            {
                throw new Exception("Invalid update shortcut");
            }

            if (duplicateUpdate.Count() != saveShortcuts.Count)
            {
                throw new Exception("Duplicate shortcut");
            }

            // check if there is any key is not valid
            foreach (var item in listKeyPress)
            {
                if (!ConstData.Const.MapKeyNumber.ContainsKey(item) && !Enum.TryParse(typeof(Key), item, out _))
                {
                    throw new Exception("Invalid key " + item);
                }
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

        public bool SaveScreenShotImageToLocal(string filePath, BitmapSource srcImage)
        {

            try
            {
                if (string.IsNullOrWhiteSpace(filePath))
                {
                    throw new Exception("Invalid file path");
                }

                string extensionFile = Path.GetExtension(filePath);
                string typeOfExtension = ConstData.Const.extenstionFilePair.FirstOrDefault(pair => pair.Key.Contains(extensionFile, StringComparer.OrdinalIgnoreCase)).Value;
                // check file has extension is image or not
                if (string.IsNullOrWhiteSpace(extensionFile) || string.IsNullOrWhiteSpace(typeOfExtension))
                {
                    Log.Warning("Invalid file path or file is not save as image: file extension - " + extensionFile + " or type of extension - " + typeOfExtension);
                    throw new Exception("Invalid file path or file is not save as image: file extension - " + extensionFile + " or type of extension - " + typeOfExtension);
                }

                BitmapEncoder? encoder = null;
                switch (typeOfExtension)
                {
                    case "tiff":
                        encoder = new TiffBitmapEncoder();
                        break;
                    case "bmp":
                        encoder = new BmpBitmapEncoder();
                        break;
                    case "png":
                        encoder = new PngBitmapEncoder();
                        break;
                    case "jpeg":
                        encoder = new JpegBitmapEncoder();
                        break;
                    case "gif":
                        encoder = new GifBitmapEncoder();
                        break;
                    default:
                        Log.Warning("May not support that file extension " + typeOfExtension);
                        throw new Exception("May not support that file extension " + typeOfExtension);
                }

                encoder.Frames.Add(BitmapFrame.Create(srcImage));
                using (var fileStream = new System.IO.FileStream(filePath, System.IO.FileMode.Create))
                {
                    encoder.Save(fileStream);
                }

                return true;
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error save image");
                throw;
            }
        }

        public void SaveCurrentUsageData(UsageModel usageModel)
        {
            Log.Information("Start saveCurrentUsageData");
            try
            {
                Log.Information($"current usage: {usageModel.currentValue}");
                var path = ConstData.Const.pathUsageData;
                var folderPath = Path.GetDirectoryName(path);

                if (string.IsNullOrWhiteSpace(folderPath))
                {
                    Log.Warning("Folder path is empty");
                    throw new ArgumentNullException(nameof(folderPath) + " is null");
                }

                // create a new folder if not exist
                if (!Directory.Exists(folderPath))
                {
                    Log.Information(folderPath + " is not exist -> create new");
                    Directory.CreateDirectory(folderPath);
                }

                var json = JsonConvert.SerializeObject(usageModel);

                using (var file = new FileStream(path, FileMode.OpenOrCreate))
                {
                    using (var sw = new StreamWriter(file))
                    {
                        sw.WriteLine(json);
                    }
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error when save current usage");
                throw;
            }
            Log.Information("end saveCurrentUsageData");
        }

        public UsageModel GetCurrentUsageData()
        {
            Log.Information("Start GetCurrentUsageData");
            try
            {
                var path = ConstData.Const.pathUsageData;

                if (!File.Exists(path))
                {
                    Log.Information("Not exist file usage data");
                    return new UsageModel
                    {
                        currentValue = 0,
                        limitValue = 0
                    };
                }

                // read data
                string json = File.ReadAllText(path);
                var model = JsonConvert.DeserializeObject<UsageModel>(json);
                if (model == null)
                {
                    Log.Information("Not have any data in usage file");
                    return new UsageModel
                    {
                        currentValue = 0,
                        limitValue = 0
                    };
                }

                Log.Information("end GetCurrentUsageData");

                return model;

            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error when get current usage");
                throw;
            }
        }
    }
}
