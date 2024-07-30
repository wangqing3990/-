namespace 温度监测程序.MonitoringSystem.pojo
{
    public class ModbusClass
    {
        private byte slaveAddress;

        private ushort startRegister;

        private ushort[] readData;

        private byte[] dataSend;

        private ushort writeCount;

        private int seat;

        private object[] order;

        private int ModbusOrder;

        public int Seat => seat;

        public int ModbusOrder1 => ModbusOrder;

        public object[] Order => order;

        public byte SlaveAddress => slaveAddress;

        public ushort StartRegister => startRegister;

        public ushort[] ReadData => readData;

        public ushort WriteCount
        {
            get
            {
                return writeCount;
            }
            set
            {
                writeCount = value;
            }
        }

        public byte[] DataSend => dataSend;

        public ModbusClass()
        {
        }

        public ModbusClass(byte slaveAddress, ushort startRegister, ushort writeCount, int ModbusOrder, int seat, object[] remark)
        {
            this.slaveAddress = slaveAddress;
            this.startRegister = startRegister;
            this.writeCount = writeCount;
            this.ModbusOrder = ModbusOrder;
            this.seat = seat;
            order = remark;
        }

        public ModbusClass(byte slaveAddress, ushort startRegister, ushort WriteCount, int ModbusOrder, int seat)
        {
            this.slaveAddress = slaveAddress;
            this.startRegister = startRegister;
            this.WriteCount = WriteCount;
            this.ModbusOrder = ModbusOrder;
            this.seat = seat;
        }

        public ModbusClass(byte slaveAddress, ushort startRegister, ushort[] readData, int ModbusOrder, int seat, object[] remark)
        {
            this.slaveAddress = slaveAddress;
            this.startRegister = startRegister;
            this.ModbusOrder = ModbusOrder;
            this.readData = readData;
            this.ModbusOrder = ModbusOrder;
            this.seat = seat;
            order = remark;
        }

        public ModbusClass(byte slaveAddress, ushort startRegister, ushort[] readData, int ModbusOrder, int seat)
        {
            this.slaveAddress = slaveAddress;
            this.startRegister = startRegister;
            this.ModbusOrder = ModbusOrder;
            this.readData = readData;
            this.ModbusOrder = ModbusOrder;
            this.seat = seat;
        }

        public ModbusClass(byte[] anyData, int seat)
        {
            dataSend = anyData;
            this.seat = seat;
        }

        public ModbusClass(byte[] anyData, int seat, object[] remark)
        {
            dataSend = anyData;
            this.seat = seat;
            order = remark;
        }
    }

}
