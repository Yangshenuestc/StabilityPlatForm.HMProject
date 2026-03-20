using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace StabilityPlatForm.HMProject.DataAccessLayer.DatabaseOperations
{
    /// <summary>
    /// IV数据表定义
    /// </summary>
    //指定在MySQL中生成的表名为"IvCurves"
    //表名
    [Table("IvCurves")]
    public class IvCurveEntity
    {
        //标记Id字段为数据库表的主键
        [Key]
        //值是自动生成的（即自增列），每次插入新数据时，数据库会自动为Id分配一个递增的整数
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }


        /// <summary>
        /// 标记仓体
        /// </summary>
        public string TaskId { get; set; }
        /// <summary>
        /// 标记器件ID
        /// </summary>
        public string DeviceId { get; set; }
        /// <summary>
        /// 标记运行时间
        /// </summary>
        public double TimeHours { get; set; }
        /// <summary>
        /// 标记正反扫，正扫(true/1)还是反扫(false/0)
        /// </summary>
        public bool SweepDirection { get; set; }
        /// <summary>
        /// 使用 longtext 类型存储可能非常长的 JSON 数组字符串（电流以及电压数据）
        /// </summary>
        [Column(TypeName = "longtext")]
        public string VoltageDataJson { get; set; }

        [Column(TypeName = "longtext")]
        public string CurrentDataJson { get; set; }
    }
}