public class JiraHelper
{
    Logger logger = LogManager.GetCurrentClassLogger();
    private WebClient client;

    private WebClient prepareJiraClient()
    {
        logger.Info("perparing Jira Client");
        client = new WebClient();
        client.Encoding = Encoding.UTF8;
        client.Headers.Set("Authorization", "Basic " + getEncodedCredentials("YOURUSERNAME"
            , "YOURPASS" ?? throw new Exception("pass is null")));
        client.Headers.Add("Content-Type", "application/json");
        client.Headers.Add("Accept", "application/json");
        return client;
    }

    public IssueRoot QueryJiraForIssues(string query)
    {
        bool acceptableProblems = true;
        while (acceptableProblems)
        {
            try
            {
                var subResponse = client.DownloadString(query);
                return JsonConvert.DeserializeObject<IssueRoot>(subResponse);
            }
            catch (Exception ex)
            {
                logger.Error(ex);
                Thread.Sleep(5000);
            }
        }
        return null;
    }

    public void CreateTask(string summary, double estimation, string owner)
    {
        client.UploadString(JiraConstants.JIRA_RESTAPI_CORE_URL + "/2/issue/", "POST", generateTaskJson(summary, estimation, owner));
    }

    private string generateTaskJson(string summary, double timeEstimation, string assignee)
    {
        return @" {	
                ""fields"":{	
                    ""project"":{""key"": ""MBSM""},	
                    ""summary"": "" " + summary + @" "",
                    ""description"": """ + summary + @""",	
                    ""issuetype"": {""name"": ""Task""},	
                    ""assignee"":{""name"":""" + assignee + @"""},	
                    ""customfield_10113"":{""value"":""System""},
                    ""timetracking"":{""originalEstimate"":""" + timeEstimation + @"d""}
                }
            }";
    }


    public OwnerLog CalculateYesterdaysLogs(string query, string candidate)
    {
        IssueRoot root = QueryJiraForIssues(query);
        return calculateYesterdaysLogs(root, candidate);
    }

    private OwnerLog calculateYesterdaysLogs(IssueRoot issueRoot, string candidate)
    {
        int yesterdaysTotalSeconds = 0;
        if (issueRoot.issues.Any())
        {
            //loop through issues
            foreach (WorkLogIssue issue in issueRoot.issues)
            {
                var response = client.DownloadString(JiraConstants.JIRA_RESTAPI_CORE_URL + "/2/issue/" + issue.key + "/worklog");
                var worklogRoot = JsonConvert.DeserializeObject<WorklogRoot>(response);
                try
                {
                    if (worklogRoot.worklogs.Any())
                    {
                        //loop through worklog in issue
                        foreach (Worklog coreLog in worklogRoot.worklogs)
                        {
                            if (coreLog.started.Day == CoreConstants.YESTERDAY_DAY)
                            {
                                yesterdaysTotalSeconds += coreLog.timeSpentSeconds;
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    logger.Error(ex);
                }
            }
        }

        return new OwnerLog { ownerUserName = candidate, yesterdaysWorkLog = yesterdaysTotalSeconds };
    }

    public JiraHelper()
    {
        if (client == null)
        {
            prepareJiraClient();
        }
    }

    /// <summary>
    /// Jira needs encoded credentials
    /// </summary>
    /// <param name="userId"></param>
    /// <param name="pass"></param>
    /// <returns></returns>
    private static string getEncodedCredentials(string userId, string pass)
    {
        string mergedCredentials = string.Format("{0}:{1}", userId, pass);
        byte[] byteCreds = UTF8Encoding.UTF8.GetBytes(mergedCredentials);
        return Convert.ToBase64String(byteCreds);
    }

    public string QueryJira(string query)
    {
        bool acceptableProblems = true;
        while (acceptableProblems)
        {
            try
            {
                return client.DownloadString(query); 
            }
            catch (Exception ex)
            {
                logger.Error(ex);
                Thread.Sleep(5000);
            }
        }
        return null;
    }
     
}
