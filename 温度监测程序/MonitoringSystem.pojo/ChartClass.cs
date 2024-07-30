using System.Windows.Forms.DataVisualization.Charting;

namespace 温度监测程序.MonitoringSystem.pojo
{
    internal class ChartClass
    {
        private int showCount;

        private float[] dataBuf;

        private bool InitializeBool = true;

        private int dataCount;

        private int bufIndex;

        public ChartClass(int showCount)
        {
            this.showCount = showCount;
            dataBuf = new float[showCount];
        }

        public void PointDisp(float data, Series name)
        {
            name.Points.Clear();
            dataBuf[bufIndex] = data;
            int j = 0;
            if (InitializeBool)
            {
                if (++dataCount == showCount)
                {
                    InitializeBool = false;
                }
            }
            else
            {
                j = bufIndex + 1;
            }
            int i = 0;
            while (i < dataCount)
            {
                if (j == showCount)
                {
                    j = 0;
                }
                name.Points.AddXY(i, dataBuf[j]);
                i++;
                j++;
            }
            if (++bufIndex == showCount)
            {
                bufIndex = 0;
            }
        }

        public void clearBuf(Series name)
        {
            name.Points.Clear();
        }
    }
}
