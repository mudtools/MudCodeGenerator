namespace CodeGeneratorTest.Options
{
    /// <summary>
    /// 租户配置.
    /// </summary>
    public sealed class TenantOptions
    {
        /// <summary>
        /// 是否多租户模式.
        /// </summary>
        public bool MultiTenancy { get; set; }

        /// <summary>
        /// 数据隔离类型 SCHEMA:库隔离 COLUMN:字段隔离.
        /// </summary>
        public string MultiTenancyType { get; set; }

        /// <summary>
        /// 多租户数据接口.
        /// </summary>
        public string MultiTenancyDBInterFace { get; set; }
    }
}
