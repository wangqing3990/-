using System;
using System.Drawing;
using System.IO.Ports;
using System.Net.Sockets;
using System.Text;
using System.Windows.Forms;

namespace AGM监测程序.MonitoringSystem.common
{
    internal class ModbusDataExhibit
    {
        public string WriteResponseOne(SerialPort port)
        {
            byte[] ndata;
            try
            {
                int count = port.BytesToRead;
                if (count <= 0)
                {
                    return null;
                }
                byte[] headeData = new byte[3];
                port.Read(headeData, 0, headeData.Length);
                byte[] bodyData = null;
                bodyData = ((headeData[1] != 4) ? new byte[count] : new byte[headeData[2] + 2]);
                port.Read(bodyData, 0, bodyData.Length);
                ndata = new byte[headeData.Length + bodyData.Length];
                Array.Copy(headeData, 0, ndata, 0, headeData.Length);
                Array.Copy(bodyData, 0, ndata, headeData.Length, bodyData.Length);
            }
            catch (Exception)
            {
                return null;
            }
            return getExhibitDataNoCRC(ndata, "←收");
        }

        public string originalData(NetworkStream port)
        {
            byte[] ndata = null;
            if (port.DataAvailable)
            {
                byte[] headeWrite = new byte[6];
                byte[] bodyData;
                try
                {
                    port.Read(headeWrite, 0, headeWrite.Length);
                    bodyData = new byte[headeWrite[5]];
                    port.Read(bodyData, 0, bodyData.Length);
                }
                catch (Exception)
                {
                    return null;
                }
                ndata = new byte[headeWrite.Length + bodyData.Length];
                Array.Copy(headeWrite, 0, ndata, 0, headeWrite.Length);
                Array.Copy(bodyData, 0, ndata, headeWrite.Length, bodyData.Length);
                return getExhibitDataNoCRC(ndata, "←收");
            }
            return null;
        }

        public byte[] Byte1And6Data(byte slaveAddress, byte functionCode, ushort startAddress, ushort data)
        {
            byte[] body = new byte[6]
            {
            slaveAddress,
            functionCode,
            (byte)(startAddress >> 8),
            (byte)startAddress,
            (byte)(data >> 8),
            (byte)data
            };
            byte[] crc = getCrc16(body);
            return getHByte(body, crc);
        }

        public string String1And6Data(byte slaveAddress, byte functionCode, ushort startAddress, ushort data, string name)
        {
            byte[] body = Byte1And6Data(slaveAddress, functionCode, startAddress, data);
            return getExhibitDataNoCRC(body, name);
        }

        public byte[] Byte1And2Data(byte slaveAddress, byte functionCode, ushort[] countData)
        {
            byte[] body = new byte[countData.Length * 2 + 3];
            body[0] = slaveAddress;
            body[1] = functionCode;
            body[2] = (byte)(countData.Length * 2);
            byte[] content = ushortByte(countData);
            Buffer.BlockCopy(body, 3, content, 0, content.Length);
            byte[] crc = getCrc16(body);
            return getHByte(body, crc);
        }

        public string String1And2Data(byte slaveAddress, byte functionCode, ushort[] countData, string name)
        {
            byte[] body = Byte1And2Data(slaveAddress, functionCode, countData);
            return getExhibitDataNoCRC(body, name);
        }

        public byte[] Byte3And4Data(byte slaveAddress, byte functionCode, ushort[] countData)
        {
            byte[] body = new byte[countData.Length * 2 + 3];
            body[0] = slaveAddress;
            body[1] = functionCode;
            body[2] = (byte)(countData.Length * 2);
            int j = 3;
            foreach (ushort data in countData)
            {
                body[j++] = (byte)(data / 256);
                body[j++] = (byte)(data % 256);
            }
            byte[] crc = getCrc16(body);
            return getHByte(body, crc);
        }

        public string String3And4Data(byte slaveAddress, byte functionCode, ushort[] countData, string name)
        {
            byte[] body = Byte3And4Data(slaveAddress, functionCode, countData);
            return getExhibitDataNoCRC(body, name);
        }

        public byte[] muchCoil(byte slaveAddress, byte functionCode, ushort startAddress, byte[] countData, ushort count)
        {
            byte byteCount = (byte)countData.Length;
            byte[] requestFrame = new byte[7 + byteCount];
            requestFrame[0] = slaveAddress;
            requestFrame[1] = functionCode;
            requestFrame[2] = (byte)(startAddress >> 8);
            requestFrame[3] = (byte)startAddress;
            requestFrame[4] = (byte)(count >> 8);
            requestFrame[5] = (byte)count;
            requestFrame[6] = byteCount;
            Array.Copy(countData, 0, requestFrame, 7, byteCount);
            byte[] crc = getCrc16(requestFrame);
            return getHByte(requestFrame, crc);
        }

        public string muchCoil(byte slaveAddress, byte functionCode, ushort startAddress, byte[] countData, ushort count, string name)
        {
            byte[] body = muchCoil(slaveAddress, functionCode, startAddress, countData, count);
            return getExhibitDataNoCRC(body, name);
        }

        public byte[] getReturnOne(byte slaveAddress, byte functionCode, ushort startAddress, ushort[] countData)
        {
            byte[] body = new byte[countData.Length * 2 + 7];
            body[0] = slaveAddress;
            body[1] = functionCode;
            body[2] = (byte)(startAddress / 256);
            body[3] = (byte)(startAddress % 256);
            ushort countLength = (ushort)countData.Length;
            body[4] = (byte)(countLength / 256);
            body[5] = (byte)(countLength % 256);
            body[6] = (byte)(countData.Length * 2);
            int j = 7;
            foreach (ushort data in countData)
            {
                body[j++] = (byte)(data / 256);
                body[j++] = (byte)(data % 256);
            }
            byte[] crc = getCrc16(body);
            return getHByte(body, crc);
        }

        public string getReturnOne(byte slaveAddress, byte functionCode, ushort startAddress, ushort[] countData, string name)
        {
            byte[] body = getReturnOne(slaveAddress, functionCode, startAddress, countData);
            return getExhibitDataNoCRC(body, name);
        }

        private ushort[] byteUshort(byte[] data)
        {
            if (data == null || data.Length == 0)
            {
                return null;
            }
            ushort[] ushortArray = new ushort[(data.Length + 1) / 2];
            for (int i = 0; i < ushortArray.Length; i++)
            {
                ushortArray[i] = BitConverter.ToUInt16(data, i * 2);
            }
            return ushortArray;
        }

        private byte[] ushortByte(ushort[] data)
        {
            if (data == null || data.Length == 0)
            {
                return null;
            }
            byte[] byteArray = new byte[data.Length * 2];
            for (int i = 0; i < data.Length; i++)
            {
                byte[] bytes = BitConverter.GetBytes(data[i]);
                Array.Copy(bytes, 0, byteArray, i * 2, 2);
            }
            return byteArray;
        }

        public string getExhibitDataNoCRC(byte[] body, string name)
        {
            if (body == null || body.Length == 0)
            {
                return null;
            }
            StringBuilder LookName = new StringBuilder();
            LookName.Append(GetTimeStamp() + name);
            LookName.Append(" ");
            foreach (byte run in body)
            {
                LookName.Append(run.ToString("X2").ToUpper());
                LookName.Append(" ");
            }
            return LookName.ToString();
        }

        public string getExhibitDataNoCRC(ushort[] bodyData, string name)
        {
            byte[] body = ushortByte(bodyData);
            StringBuilder LookName = new StringBuilder();
            LookName.Append(GetTimeStamp() + name);
            LookName.Append(" ");
            byte[] array = body;
            foreach (byte run in array)
            {
                LookName.Append(run.ToString("X2").ToUpper());
                LookName.Append(" ");
            }
            return LookName.ToString();
        }

        public byte[] getHByte(byte[] body, byte[] crcData)
        {
            byte[] dataAnd = new byte[body.Length + crcData.Length];
            body.CopyTo(dataAnd, 0);
            crcData.CopyTo(dataAnd, body.Length);
            return dataAnd;
        }

        public byte[] getCrc16(byte[] arr_buff)
        {
            ushort crc = ushort.MaxValue;
            for (int i = 0; i < arr_buff.Length; i++)
            {
                crc ^= (ushort)(arr_buff[i] & 0xFFu);
                for (int j = 0; j < 8; j++)
                {
                    crc = (((crc & 1) == 0) ? ((ushort)(crc >> 1)) : ((ushort)((uint)(crc >> 1) ^ 0xA001u)));
                }
            }
            return new byte[2]
            {
            (byte)(crc & 0xFFu),
            (byte)(crc >> 8)
            };
        }

        public string GetTimeStamp()
        {
            return DateTime.Now.ToString("HH:mm:ss.fff");
        }

        public void AddTextBox(RichTextBox LooktextBox, string ExhibitData, string indexof, Color color)
        {
            if (!string.IsNullOrEmpty(ExhibitData))
            {
                int index = ExhibitData.IndexOf(indexof);
                if (index >= 0)
                {
                    LooktextBox.SelectionStart = LooktextBox.TextLength;
                    LooktextBox.SelectionColor = color;
                    string prefix = ExhibitData.Substring(0, index + 1);
                    string suffix = ExhibitData.Substring(index + 1);
                    LooktextBox.SelectedText = prefix;
                    LooktextBox.SelectionColor = Color.White;
                    LooktextBox.SelectedText = suffix + "\r\n";
                    LooktextBox.ScrollToCaret();
                }
            }
        }
    }
}
