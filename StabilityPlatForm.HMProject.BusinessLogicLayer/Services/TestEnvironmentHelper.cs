using StabilityPlatForm.HMProject.DataAccessLayer.HardwareDriverImplementation;
using StabilityPlatForm.HMProject.Models.Enumeration;
using StabilityPlatForm.HMProject.Models.Hardwcare;
using StabilityPlatForm.HMProject.Models.Interfaces;
using StabilityPlatForm.HMProject.Utility;

namespace StabilityPlatForm.HMProject.BusinessLogicLayer.Services
{
    /// <summary>
    /// 测试模式环境配置：专门用于封装和隔离繁杂的硬件仪器调用逻辑
    /// </summary>
    public class TestEnvironmentHelper
    {
        //必选：光源、偏压表、通道切换器、源表
        private readonly ISourceTable _sourceTable;
        private readonly IChannelSwitcher _channelSwitcher;
        private readonly ILightSource _lightSource;
        private readonly IBiasSourceTable _biasSourceTable;

        //视测试条件而定：半导体温控台
        private readonly ISemiconductor _semiconductor;

        public TestEnvironmentHelper(
            ISourceTable sourceTable,
            IChannelSwitcher channelSwitcher,
            ISemiconductor semiconductor,
            ILightSource lightSource,
            IBiasSourceTable biasSourceTable)
        {
            _sourceTable = sourceTable;
            _channelSwitcher = channelSwitcher;
            _semiconductor = semiconductor;
            _lightSource = lightSource;
            _biasSourceTable = biasSourceTable;
        }

        /// <summary>
        /// 启动所有基础硬件连接并检查连接状态
        /// </summary>
        public void TurnOnBaseDevices(TestParameter config)
        {
            try
            {
                // 声明一个变量用于接收每次调用的返回结果
                MethodResult<bool> result;

                // 1. 启动源表
                result = _sourceTable.Start();
                if (!result.IsSuccessful)
                {
                    throw new Exception($"源表未连接: {result.Msg}");
                }

                // 2. 启动通道切换器
                result = _channelSwitcher.Start();
                if (!result.IsSuccessful)
                {
                    throw new Exception($"通道切换器未连接: {result.Msg}");
                }

                // 3. 启动偏压源表
                result = _biasSourceTable.Start();
                if (!result.IsSuccessful)
                {
                    throw new Exception($"偏压源表未连接: {result.Msg}");
                }

                // 4. 启动光源
                result = _lightSource.Start();
                if (!result.IsSuccessful)
                {
                    throw new Exception($"光源未连接: {result.Msg}");
                }

                // 5. 按需启动半导体温控模块
                if (config.SelectedTestMode == TestMode.Mode_2 || config.SelectedTestMode == TestMode.Mode_3)
                {
                    result = _semiconductor.Start();
                    if (!result.IsSuccessful)
                    {
                        throw new Exception($"半导体温控台未连接: {result.Msg}");
                    }
                }
            }
            catch (Exception e)
            {
                throw new Exception("连接硬件失败：" + e.Message);
            }
        }

        /// <summary>
        /// 根据测试模式，精准配置测试环境
        /// </summary>
        public void ConfigureTestEnvironment(TestParameter config)
        {
            //无论是哪种模式，都会施加偏压
            _biasSourceTable.TestMode_Vmpp(new BiasInfo { Vmpp = config.AppliedVoltage });

            switch (config.SelectedTestMode)
            {
                case TestMode.Mode_1:
                    // 仅设置光暗循环+偏压
                    _lightSource.SetLightControl(new LightInfo { LightTime = config.SunTime, DarkTime = config.DarkTime });
                    _lightSource.StartWork();
                    break;

                case TestMode.Mode_2:
                    // 24小时常亮 + 恒温 + 偏压
                    _lightSource.SetLightControl(new LightInfo { LightTime = 24, DarkTime = 0 });
                    _lightSource.StartWork();
                    _semiconductor.TemperatureControl(new TemperatureInfo { TargetT = config.TargetTemperature }, TestMode.Mode_2);
                    break;

                case TestMode.Mode_3:
                default:
                    // 24小时常亮 + 变温循环 + 偏压
                    _lightSource.SetLightControl(new LightInfo { LightTime = 24, DarkTime = 0 });
                    _lightSource.StartWork();
                    _semiconductor.TemperatureControl(new TemperatureInfo { TargetT = config.TargetTemperature }, TestMode.Mode_2);
                    break;
            }
        }

        /// <summary>
        /// 停止所有硬件工作并安全复位
        /// </summary>
        public void ResetAllDevices(TestParameter config)
        {
            try
            {
                MethodResult<bool> result;
                // 1. 复位光源
                result = _lightSource.StopWork();
                if (!result.IsSuccessful)
                {
                    throw new Exception($"光源未复位: {result.Msg}");
                }
                // 2. 复位偏压源表
                result = _biasSourceTable.StopTest();
                if (!result.IsSuccessful)
                {
                    throw new Exception($"偏压源表未复位: {result.Msg}");
                }
                // 3. 复位通道切换器
                result = _channelSwitcher.StopWork();
                if (!result.IsSuccessful)
                {
                    throw new Exception($"通道切换器未复位: {result.Msg}");
                }
                // 4. 复位源表
                result = _sourceTable.StopTest();
                if (!result.IsSuccessful)
                {
                    throw new Exception($"源表未复位: {result.Msg}");
                }
                // 5.按需复位半导体温控台
                if(config.SelectedTestMode == TestMode.Mode_2 || config.SelectedTestMode == TestMode.Mode_3)
                {
                    result = _semiconductor.StopWork();
                    if(!result.IsSuccessful)
                    {
                        throw new Exception($"半导体未复位: {result.Msg}");
                    }
                }
            }
            catch (Exception e)
            {
                throw new Exception("复位硬件失败：" + e.Message);
            }
        }
        /// <summary>
        /// 关闭所有设备连接并清理缓存
        /// </summary>
        /// <param name="config"></param>
        public void CloseAllDevices(TestParameter config)
        {
            _lightSource.Close();
            _biasSourceTable.Close();
            _channelSwitcher.Close();
            _sourceTable.Close();
            if(config.SelectedTestMode == TestMode.Mode_2 || config.SelectedTestMode == TestMode.Mode_3)
            {
                _semiconductor.Close();
            }
        }
    }
}