using Microsoft.EntityFrameworkCore;

namespace StabilityPlatForm.HMProject.DataAccessLayer.DatabaseOperations
{
    /// <summary>
    /// C#与MySQL数据库之间沟通，所有针对数据库的增、删、改、查操作，都要通过这个类来完成
    /// </summary>
    public class HMDatabaseContext : DbContext
    {
        //注册StabilityResult表
        public DbSet<StabilityResultEntity> StabilityResults { get; set; }

        //注册IV数据表
        public DbSet<IvCurveEntity> IvCurves { get; set; }
        /// <summary>
        /// 这个方法在实例被创建时自动调用，告诉EF Core “去哪里连接数据库”以及“使用什么数据库引擎”。
        /// </summary>
        /// <param name="optionsBuilder"></param>
        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            //配置MySQL连接的字符串
            string connectionString = "Server=localhost;Port=3306;Database=hm_lab_stability;Uid=root;Pwd=123456;";

            // 告诉 EF Core 使用 MySQL 驱动
            optionsBuilder.UseMySql(connectionString, ServerVersion.AutoDetect(connectionString));
        }
    }
}