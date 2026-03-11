namespace RevitHydraulicPlugin.Configuration
{
    /// <summary>
    /// Configurações gerais do plugin, centralizando parâmetros ajustáveis.
    /// Em versões futuras, estas configurações podem ser lidas de arquivo externo.
    /// </summary>
    public static class PluginSettings
    {
        /// <summary>
        /// Offset horizontal (em milímetros) da coluna em relação ao centro do ambiente.
        /// Usado quando não há shaft definido.
        /// </summary>
        public static double ColumnOffsetFromCenterMm => 300;

        /// <summary>
        /// Offset vertical (em milímetros) da tubulação de ramal em relação ao nível do piso.
        /// Ramais de esgoto geralmente ficam embutidos no piso.
        /// </summary>
        public static double BranchHeightOffsetMm => -50;

        /// <summary>
        /// Offset vertical (em milímetros) para ramais de água fria.
        /// Ramais de água fria geralmente ficam a uma certa altura da parede.
        /// </summary>
        public static double ColdWaterBranchHeightMm => 600;

        /// <summary>
        /// Tolerância (em milímetros) para detecção de proximidade entre elementos.
        /// </summary>
        public static double ProximityToleranceMm => 100;

        /// <summary>
        /// Extensão vertical extra (em milímetros) da coluna acima do último nível.
        /// Usado para ventilação de colunas de esgoto.
        /// </summary>
        public static double ColumnExtensionAboveTopMm => 500;

        /// <summary>
        /// Extensão vertical extra (em milímetros) da coluna abaixo do primeiro nível.
        /// Usado para conexão com rede primária.
        /// </summary>
        public static double ColumnExtensionBelowBottomMm => 300;

        /// <summary>
        /// Nome do parâmetro no Revit para obter o nome do Room.
        /// </summary>
        public static string RoomNameParameter => "Name";

        /// <summary>
        /// Nome do parâmetro no Revit para obter o número do Room.
        /// </summary>
        public static string RoomNumberParameter => "Number";
    }
}
