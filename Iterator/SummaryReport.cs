using System.Collections.Generic;

namespace Iterator
{
    public class SummaryReport
    {
        public SummaryReport(List<string> data = null)
        {
            if (data != null && data.Count == 17)
            {
                double dVal = 0;
                int iVal = 0;

                if (double.TryParse(data[0], out dVal)) CapacityGrowthRate = dVal;
                if (double.TryParse(data[1], out dVal)) DemandGrowthRate = dVal;
                if (int.TryParse(data[2], out iVal)) Aircrafts = iVal; else if (int.TryParse(data[2].Replace(".",""), out iVal)) Aircrafts = iVal;
                if (int.TryParse(data[3], out iVal)) AircraftAcquisition = iVal; else if (int.TryParse(data[3].Replace(".", ""), out iVal)) AircraftAcquisition = iVal;
                if (double.TryParse(data[4], out dVal)) LoadFactor = dVal;
                if (double.TryParse(data[5], out dVal)) BreakevenLoadFactor = dVal;
                if (double.TryParse(data[6], out dVal)) Fare = dVal;
                if (double.TryParse(data[7], out dVal)) CompetitorFare = dVal;
                if (int.TryParse(data[8], out iVal)) Employees = iVal; else if (int.TryParse(data[8].Replace(".", ""), out iVal)) Employees = iVal;
                if (int.TryParse(data[9], out iVal)) EmployeesPerPlane = iVal; else if (int.TryParse(data[9].Replace(".", ""), out iVal)) EmployeesPerPlane = iVal;
                if (int.TryParse(data[10], out iVal)) Hiring = iVal; else if (int.TryParse(data[10].Replace(".", ""), out iVal)) Hiring = iVal;
                if (int.TryParse(data[11], out iVal)) Turnover = iVal; else if (int.TryParse(data[11].Replace(".", ""), out iVal)) Turnover = iVal;
                if (double.TryParse(data[12], out dVal)) Marketing = dVal;
                if (double.TryParse(data[13], out dVal)) MarketShare = dVal;
                if (double.TryParse(data[14], out dVal)) ServiceQuality = dVal;
                if (double.TryParse(data[15], out dVal)) Revenue = dVal;
                if (double.TryParse(data[16], out dVal)) NetIncome = dVal;
            }
        }

        public double CapacityGrowthRate { get; private set; }
        public double DemandGrowthRate { get; private set; }
        public int Aircrafts { get; private set; }
        public int AircraftAcquisition { get; private set; }
        public double LoadFactor { get; private set; }
        public double BreakevenLoadFactor { get; private set; }
        public double Fare { get; private set; }
        public double CompetitorFare { get; private set; }
        public int Employees { get; private set; }
        public int EmployeesPerPlane { get; private set; }
        public int Hiring { get; private set; }
        public int Turnover { get; private set; }
        public double Marketing { get; private set; }
        public double MarketShare { get; private set; }
        public double ServiceQuality { get; private set; }
        public double Revenue { get; private set; }
        public double NetIncome { get; private set; }

        public List<string> Values
        {
            get
            {
                var list = new List<string>();
                foreach (var prop in GetType().GetProperties())
                    if (!prop.Name.Equals("Values"))
                        list.Add(prop.GetValue(this, null).ToString());
                return list;
            }
        }

        public List<string> ToList()
        {
            var list = new List<string>();
            foreach (var prop in GetType().GetProperties())
                if (!prop.Name.Equals("Values"))
                    list.Add(prop.Name +": " + prop.GetValue(this, null).ToString());
            return list;
        }
    }
}
