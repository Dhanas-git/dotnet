#region Copyright © 2017 Inovalon
//
// All rights are reserved. Reproduction or transmission in whole or in part, in
// any form or by any means, electronic, mechanical or otherwise, is prohibited
// without the prior written consent of the copyright owner.
//
#endregion


using System.Collections.Generic;

namespace Orchestration.Tasks.Models
{
    public class RatesCacheModel : CacheModel, IRatesCacheModel
    {

        #region Public Properties

        public string flowchartCatalogPopulations { get; set; }

        public List<int> hrSampleMeasureIDs { get; set; }

        #endregion

    }
}
