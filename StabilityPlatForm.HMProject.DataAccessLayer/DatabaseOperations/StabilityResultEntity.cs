using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace StabilityPlatForm.HMProject.DataAccessLayer.DatabaseOperations
{
    /// <summary>
    /// 稳定性结果表格定义
    /// </summary>
    // 指定在 MySQL 中生成的表名为 "StabilityResults"
    [Table("StabilityResults")]
    public class StabilityResultEntity
    {
        // 设置为主键，并配置为自增
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }



        // 测试任务编号 (用于区分不同批次的测试，如 "Cavity1_20260401")
        public string TaskId { get; set; }
        public string DeviceId { get; set; }
        public double TimeHours { get; set; }
        /// <summary>
        /// 最大功率点
        /// </summary>
        public double Pmax { get; set; }
        /// <summary>
        /// 开路电压
        /// </summary>
        public double Voc { get; set; }
        /// <summary>
        /// 短路电流
        /// </summary>
        public double Jsc { get; set; }
        /// <summary>
        /// 填充因子
        /// </summary>
        public double FF { get; set; }
        /// <summary>
        /// 串联电阻，理想值：0
        /// </summary>
        public double Rseries { get; set; }
        /// <summary>
        /// 并联电阻，理想值：无穷大
        /// </summary>
        public double Rshunt { get; set; }
        /// <summary>
        /// 正反扫
        /// </summary>
        public bool SweepDirection { get; set; }
        /// <summary>
        /// 实时温度
        /// </summary>
        public double Temperature { get; set; }
    }
}