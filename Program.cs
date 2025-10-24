using Amazon;
using Amazon.CloudFormation;
using Amazon.CloudFormation.Model;

var client = new AmazonCloudFormationClient(RegionEndpoint.USEast1);
var stackName = "pulsemo-pulseus-infra";
var templateBody = File.ReadAllText("infra/pulsemo_pulseus_infra_template.json");

try
{
    // Check if stack exists
    await client.DescribeStacksAsync(new DescribeStacksRequest { StackName = stackName });

    // Stack exists → create and execute change set
    var changeSetName = $"callback-changeset-{DateTime.UtcNow:yyyyMMddHHmmss}";
    var changeSet = await client.CreateChangeSetAsync(new CreateChangeSetRequest
    {
        StackName = stackName,
        ChangeSetName = changeSetName,
        ChangeSetType = ChangeSetType.UPDATE,
        TemplateBody = templateBody,
        Capabilities = new List<string> { "CAPABILITY_NAMED_IAM" }
    });

    Console.WriteLine($"ChangeSet created: {changeSet.Id}");

    // Wait for ChangeSet to be ready
    DescribeChangeSetResponse changeSetDetails;
    do
    {
        await Task.Delay(5000);
        changeSetDetails = await client.DescribeChangeSetAsync(new DescribeChangeSetRequest
        {
            StackName = stackName,
            ChangeSetName = changeSetName
        });
    } while (changeSetDetails.Status == ChangeSetStatus.CREATE_IN_PROGRESS);

    if (changeSetDetails.Status == ChangeSetStatus.FAILED)
    {
        Console.WriteLine($"ChangeSet failed: {changeSetDetails.StatusReason}");
    }
    else
    {
        await client.ExecuteChangeSetAsync(new ExecuteChangeSetRequest
        {
            StackName = stackName,
            ChangeSetName = changeSetName
        });
        Console.WriteLine("ChangeSet executed.");
    }
}
catch (AmazonCloudFormationException ex) when (ex.Message.Contains("does not exist"))
{
    // Stack doesn't exist → create it
    var response = await client.CreateStackAsync(new CreateStackRequest
    {
        StackName = stackName,
        TemplateBody = templateBody,
        Capabilities = new List<string> { "CAPABILITY_NAMED_IAM" }
    });

    Console.WriteLine($"Stack created: {response.StackId}");
}
