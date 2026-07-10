using System.Threading.Tasks;

namespace Fabric_Extension_BE_Boilerplate.Services
{
    public interface ICluedInOrganizationService
    {
        Task CreateOrganization(CreateOrganizationRequest createOrganizationRequest);
    }
}