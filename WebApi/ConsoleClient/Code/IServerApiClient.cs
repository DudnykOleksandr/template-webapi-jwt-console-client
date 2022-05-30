using Refit;

namespace ConsoleClient.Code;

public interface IServerApiClient
{
    [Post("/Users/Login")]
    Task<string> LoginAsync(LoginDto loginModel);


    [Get("/Agents/Register")]
    Task<long> RegisterAgentAsync(string name, string macaddress);



    [Get("/Workers/Register")]
    Task<long> RegisterWorkerAsync(long agentId, int index);

    [Get("/Workers/GetJob")]
    Task<JobDto> GetJobAsync(long workerId);



    [Get("/JobExecution/ReportStart")]
    Task<JobExecutionDto> ReportStartAsync(long workerId, long jobId);

    [Get("/JobExecution/ReportProgress")]
    Task ReportProgressAsync(long workerId, long jobId, string details);

    [Get("/JobExecution/ReportFinish")]
    Task ReportFinishAsync(long workerId, long jobId, string details);
}