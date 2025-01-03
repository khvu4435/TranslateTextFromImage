﻿using ScanTextImage.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ScanTextImage.Interface
{
    public interface ISaveDataService
    {
        public SaveModel SaveDataToFile(SaveModel saveModel);
        public List<ShortcutModel> SaveShortcut(List<ShortcutModel> saveShortcuts);
        public void DeleteDataFile(int? id);
        public List<SaveModel> GetListSaveData();
        public List<ShortcutModel> GetShortcutConfig();
    }
}
