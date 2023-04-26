//using Newtonsoft.Json;
//using System;
//using System.Collections.Generic;
//using System.IO;
//using System.Linq;
//using System.Text;
//using System.Threading.Tasks;
//using static GetPoster.MainWindow;

//public class ContentManager
//{
//    private readonly string _dataDir;
//    private readonly string _contentFilePath;
//    private List<Content> _contents;

//    public ContentManager()
//    {
//        _dataDir = System.IO.Path.Combine(Directory.GetCurrentDirectory(), "content");
//        Directory.CreateDirectory(_dataDir);
//        _contentFilePath = System.IO.Path.Combine(_dataDir, "contents.json");
//        LoadContents();
//    }

//    private void LoadContents()
//    {
//        if (File.Exists(_contentFilePath))
//        {
//            string jsonContent = File.ReadAllText(_contentFilePath);
//            _contents = JsonConvert.DeserializeObject<List<Content>>(jsonContent) ?? new List<Content>();
//        }
//        else
//        {
//            _contents = new List<Content>();
//        }
//    }

//    private void SaveContents()
//    {
//        string jsonContentString = JsonConvert.SerializeObject(_contents, Formatting.Indented);
//        File.WriteAllText(_contentFilePath, jsonContentString);
//    }
//}
