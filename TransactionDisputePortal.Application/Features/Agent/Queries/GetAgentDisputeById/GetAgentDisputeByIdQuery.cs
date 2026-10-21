using AutoMapper;
using AutoMapper.QueryableExtensions;
using MediatR;
using Microsoft.EntityFrameworkCore;
using TransactionDisputePortal.Application.Common.Interfaces;
using TransactionDisputePortal.Application.Features.Agent.Models;

namespace TransactionDisputePortal.Application.Features.Agent.Queries.GetAgentDisputeById;

public record GetAgentDisputeByIdQuery(Guid Id) : IRequest<AgentDisputeDTO>
{
    public class GetAgentDisputeByIdQueryHandler(IMapper mapper, IApplicationDbContext applicationDbContext)
        : IRequestHandler<GetAgentDisputeByIdQuery, AgentDisputeDTO>
    {
        private readonly IMapper mapper = mapper;
        private readonly IApplicationDbContext applicationDbContext = applicationDbContext;

        public async Task<AgentDisputeDTO> Handle(GetAgentDisputeByIdQuery request, CancellationToken cancellationToken)
        {
            var dispute = await applicationDbContext.Disputes
                                .Where(dispute => dispute.Id == request.Id)
                                .ProjectTo<AgentDisputeDTO>(mapper.ConfigurationProvider)
                                .FirstAsync(cancellationToken);

            return dispute;
        }
    }
}
