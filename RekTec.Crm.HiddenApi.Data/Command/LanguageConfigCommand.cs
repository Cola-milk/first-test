#region 文件描述
/*******************************************************************
* 创建人   : Bill Ni
* 创建时间 : 2021/5/17 10:02:39
* 功能描述 : 

===================================================================
* 此技术信息为本公司机密信息，未经本公司书面同意禁止向第三方披露．
* Copyright © 2021 苏州瑞泰信息技术有限公司 All Rights Reserved.
*******************************************************************/
#endregion
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;
using RekTec.Crm.Common.Helper;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace RekTec.Crm.HiddenApi.Data
{
    public class LanguageConfigCommand : HiddenCommand
    {
        public virtual ImportResultModel LanguageConfigImport(string importLogId, string content)
        {
            ImportResultModel result = new ImportResultModel();
            if (string.IsNullOrWhiteSpace(content))
                return result;
            //解析数据结构，结构为：行、列、值
            ImportContentModel contentModel = JsonHelper.Deserialize<ImportContentModel>(content);
            result.Messages = new List<ImportMessage>();

            int i = 1;

            foreach (Dictionary<string, string> item in contentModel.Rows)
            {
                #region 数据有效性验证（包含必填项检查） test

                string name = string.Empty;
                Guid languageId = Guid.Empty, outerappId = Guid.Empty, languageConfigId = Guid.Empty;
                List<int> langId = new List<int>();
                bool isError = false;
                int count = item.Count - 3;
                int length = 0;
                foreach (string key in item.Keys)
                {
                    try
                    {
                        string value = item[key].ToString();
                        Dictionary<string, string> data = new Dictionary<string, string>();
                        if (key == "信息代码")
                        {
                            if (string.IsNullOrWhiteSpace(value))
                                throw new InvalidPluginExecutionException($"第{ i }行导入失败，原因是：信息代码{ value }为空");

                        }
                        if (key != "信息代码" && key != "信息描述" && key != "互联应用")
                        {
                            if (!IsNumeric(key))
                                throw new InvalidPluginExecutionException("语言ID有误！");
                            //if (!string.IsNullOrWhiteSpace(value))
                            langId.Add(Convert.ToInt32(key));
                        }
                    }
                    catch (InvalidPluginExecutionException ex)
                    {
                        ImportMessage import = new ImportMessage()
                        {
                            Message = ex.Message,
                            MessageType = 4
                        };
                        result.Messages.Add(import);
                        isError = true;
                    }
                }

                #endregion

                #region 数据存在异常，则跳过，不导入

                i++;

                if (isError)
                {
                    result.ErrorRecord++;
                    continue;
                }
                #endregion

                #region 创建或更新语言配置

                try
                {
                    Entity languageConfig = new Entity("new_languageconfig");
                    for (int j = 0; j < count; j++)
                    {
                        // 语言和互联应用的存在性并查询其id
                        QueryExpression queryLanguage = new QueryExpression("new_language");
                        queryLanguage.ColumnSet.AddColumn("new_languageid");
                        queryLanguage.Criteria.AddCondition("new_langid", ConditionOperator.Equal, langId[j]);
                        EntityCollection languageList = OrganizationService.RetrieveMultiple(queryLanguage);
                        if (languageList?.Entities?.Count <= 0)
                            throw new InvalidPluginExecutionException($"语言{ langId[j] }不存在！");
                        languageId = languageList.Entities[0].GetAttributeValue<Guid>("new_languageid");
                        // 互联应用为空：更新为null，有值：查找
                        if (!string.IsNullOrWhiteSpace(item["互联应用"]))
                        {
                            QueryExpression queryOuterApp = new QueryExpression("new_outerapp");
                            queryOuterApp.ColumnSet.AddColumn("new_outerappid");
                            queryOuterApp.Criteria.AddCondition("new_name", ConditionOperator.Equal, item["互联应用"]);
                            EntityCollection outerAppList = OrganizationService.RetrieveMultiple(queryOuterApp);
                            if (outerAppList?.Entities?.Count <= 0)
                                throw new InvalidPluginExecutionException("互联应用不存在！");
                            outerappId = outerAppList.Entities[0].GetAttributeValue<Guid>("new_outerappid");
                            languageConfig["new_outerapp_id"] = new EntityReference("new_outerapp", outerappId);
                        }
                        else
                        {
                            languageConfig["new_outerapp_id"] = null;
                        }
                        // 创建、更新及删除
                        
                        languageConfig["new_name"] = item["信息代码"];
                        languageConfig["new_language_id"] = new EntityReference("new_language", languageId);
                        languageConfig["new_content"] = item[langId[j].ToString()];
                        languageConfig["new_note"] = item["信息描述"];
                        // 获取语言配置
                        QueryExpression queryLanguageConfig = new QueryExpression("new_languageconfig");
                        queryLanguageConfig.ColumnSet.AddColumn("new_languageconfigid");
                        queryLanguageConfig.Criteria.AddCondition("new_name", ConditionOperator.Equal, item["信息代码"]);
                        queryLanguageConfig.Criteria.AddCondition("new_language_id", ConditionOperator.Equal, languageId);
                        EntityCollection languageConfigList = OrganizationService.RetrieveMultiple(queryLanguageConfig);
                        if (languageConfigList?.Entities?.Count <= 0)
                        {
                            if (string.IsNullOrWhiteSpace(item[langId[j].ToString()]))
                                continue;
                            OrganizationService.Create(languageConfig);
                        }
                        else
                        {
                            if (string.IsNullOrWhiteSpace(item[langId[j].ToString()]))
                            {
                                OrganizationService.Delete("new_languageconfig", languageConfigList.Entities[0].GetAttributeValue<Guid>("new_languageconfigid"));
                                continue;
                            }
                            languageConfig.Id = languageConfigList.Entities[0].GetAttributeValue<Guid>("new_languageconfigid");
                            OrganizationService.Update(languageConfig);
                        }
                    }
                }
                catch (InvalidPluginExecutionException ex)
                {
                    ImportMessage import = new ImportMessage()
                    {
                        Message = ex.Message,
                        MessageType = 4
                    };
                    result.Messages.Add(import);
                    isError = true;
                }


                #endregion
            }

            return result;
        }

        public static bool IsNumeric(string value)
        {
            return Regex.IsMatch(value, @"^[+-]?\d*[.]?\d*$");
        }
    }
    public class ImportContentModel
    {
        /// <summary>
        /// 关联主档字段名字
        /// </summary>
        public string MainAttribute { get; set; }
        /// <summary>
        /// 主档Id
        /// </summary>
        public string ParentEntityId { get; set; }
        /// <summary>
        /// 待导入的数据
        /// </summary>
        public List<Dictionary<string, string>> Rows { get; set; }
    }

    public class ImportResultModel
    {
        /// <summary>
        /// 执行结果
        /// </summary>
        public bool Result { get; set; } = true;
        /// <summary>
        /// 成功的记录数
        /// </summary>
        public int SuccessRecord { get; set; } = 0;
        /// <summary>
        /// 错误的记录数
        /// </summary>
        public int ErrorRecord { get; set; } = 0;
        /// <summary>
        /// 错误日志
        /// </summary>
        public List<ImportMessage> Messages { get; set; } = new List<ImportMessage>();
        /// <summary>
        /// 创建成功的id集合
        /// </summary>
        public List<string> RecordIds { get; set; }
    }
    public class ImportMessage
    {
        /// <summary>
        /// 消息内容
        /// </summary>
        public string Message { get; set; }
        /// <summary>
        /// 消息类型
        /// </summary>
        public int MessageType { get; set; } = (int)ImportMessageType.Normal;
    }

    public enum ImportMessageType
    {
        Normal = 1,
        Success = 2,
        Warning = 3,
        Error = 4,
    }

}
