
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace card_reader
{
    public class WorkShiftReport
    {
        public List<WorkShiftInfo> workShiftInfos { get; set; }
        public WorkShift workShift { get; set; }
        public WorkShiftReport()
        {

        }
        public WorkShiftReport(
            List<WorkShiftInfo> workShiftInfos,
            WorkShift workShift
            )
        {
            this.workShiftInfos = workShiftInfos;
            this.workShift = workShift;
        }
    }
}