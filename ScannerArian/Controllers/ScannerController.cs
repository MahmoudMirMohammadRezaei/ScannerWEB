﻿using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using ScannerMVC.Hubs;
using ScannerMVC.Models;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;

namespace ScannerMVC.Controllers
{
    public class ScannerController : Controller
    {
        private readonly IHubContext<ScannerHub> _scannerHubContext;
        private readonly IWebHostEnvironment _environment;

        public ScannerController(IHubContext<ScannerHub> scannerHubContext, IWebHostEnvironment environment)
        {
            _scannerHubContext = scannerHubContext;
            _environment = environment;
        }

        public IActionResult Index()
        {
            var model = new NewScanViewModel();
            return View(model);
        }

        [HttpPost]
        public async Task<IActionResult> DetectScanner()
        {
            await _scannerHubContext.Clients.All.SendAsync("DetectScanner", "Find First Scanner");
            return Ok();
        }

        [HttpPost]
        public async Task<IActionResult> DoScan([FromForm] NewScanViewModel model)
        {
            await _scannerHubContext.Clients.All.SendAsync("DoScan", model);
            return Ok();
        }

        [HttpPost]
        public async Task<IActionResult> GetScannedImages(IList<IFormFile> files)
        {
            const string folder = "uploads";
            var uploadsRootFolder = Path.Combine(_environment.WebRootPath, folder);
            if (!Directory.Exists(uploadsRootFolder))
            {
                Directory.CreateDirectory(uploadsRootFolder);
            }

            foreach (var file in files)
            {
                if (file == null || file.Length == 0)
                {
                    continue;
                }

                var filePath = Path.Combine(uploadsRootFolder, file.FileName);
                using (var fileStream = new FileStream(filePath, FileMode.Create))
                {
                    await file.CopyToAsync(fileStream);
                    await _scannerHubContext.Clients.All.SendAsync("OnScannedImagesReceived", $"/{folder}/{file.FileName}");
                }
            }

            return Ok();
        }
    }
}