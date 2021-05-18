#region 文件描述
/*******************************************************************
* 创建人   : Bill Ni
* 创建时间 : 2021/5/17 11:29:40
* 功能描述 : 

===================================================================
* 此技术信息为本公司机密信息，未经本公司书面同意禁止向第三方披露．
* Copyright © 2021 苏州瑞泰信息技术有限公司 All Rights Reserved.
*******************************************************************/
#endregion
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RekTec.Crm.HiddenApi.Data
{
    public class LanguageConfigController : HiddenApiController
    {
        public virtual ImportResultModel LanguageConfigImport(string importLogId, string content)
        {
            return this.Command<LanguageConfigCommand>().LanguageConfigImport(importLogId, content);
        }
    }
}
