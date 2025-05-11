using System;
using System.Collections.Generic;
using EasySaveV1.EasySaveConsole.Managers;
using EasySaveV1.EasySaveConsole.Models;
using EasySaveV1.EasySaveConsole.Views;

namespace EasySaveV1.EasySaveConsole.Controllers
{
    public class BackupController
    {
        private readonly BackupManager _manager;
        private readonly BackupView _view;
        private string _language = "en";

        public BackupController()
        {
            _manager = new BackupManager();
            _view = new BackupView();
        }
    }
}