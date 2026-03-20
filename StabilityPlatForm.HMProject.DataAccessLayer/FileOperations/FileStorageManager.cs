namespace StabilityPlatForm.HMProject.DataAccessLayer.FileOperations
{
    public class FileStorageManager
    {
        private readonly string _rootPath;

        /// <summary>
        /// 初始化文件，建立相关文件夹
        /// </summary>
        /// <param name="saveDirectory">SavePath</param>
        /// <param name="fileName">FileName</param>
        public FileStorageManager(string saveDirectory, string fileName)
        {
            // 根目录: SelectedPath\FileName\
            _rootPath = Path.Combine(saveDirectory, fileName);
            InitializeDirectories();
        }

        /// <summary>
        /// 自动创建 Forward/Reverse 及其子文件夹 IV/Stability Result
        /// </summary>
        private void InitializeDirectories()
        {
            string[] directions = { "Forward", "Reverse" };
            string[] subFolders = { "IV", "Stability Result" };

            foreach (var dir in directions)
            {
                foreach (var sub in subFolders)
                {
                    // 组合路径例如
                    string path = Path.Combine(_rootPath, dir, sub);

                    //很安全，如果文件夹已存在则不会做任何操作
                    Directory.CreateDirectory(path);
                }
            }
        }

        /// <summary>
        /// 获取原始 IV 数据的 Excel 文件路径 (例如: ...\Forward\IV\1-1.xlsx)
        /// </summary>
        public string GetIvFilePath(string direction, string deviceId)
        {
            return Path.Combine(_rootPath, direction, "IV", $"{deviceId}.xlsx");
        }

        /// <summary>
        /// 获取参数结果的 Excel 文件路径 (例如: ...\Forward\Result\1-1.xlsx)
        /// </summary>
        public string GetResultFilePath(string direction, string deviceId)
        {
            return Path.Combine(_rootPath, direction, "Stability Result", $"{deviceId}.xlsx");
        }
    }
}
