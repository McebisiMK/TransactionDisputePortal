using MediatR;
using TransactionDisputePortal.Application.Common.Extensions;
using TransactionDisputePortal.Application.Common.Models;
using TransactionDisputePortal.Application.Features.Lookups.Models;
using TransactionDisputePortal.Domain.Enums;

namespace TransactionDisputePortal.Application.Features.Lookups.Queries.GetLookups;

public record GetLookupsQuery : IRequest<LookupsDTO>
{
    public class GetLookupsQueryHandler : IRequestHandler<GetLookupsQuery, LookupsDTO>
    {
        private static readonly LookupsDTO Lookups = new()
        {
            DisputeReasons = CreateLookup<DisputeReason>(),
            DisputeStatuses = CreateLookup<DisputeStatus>(),
            TransactionStatuses = CreateLookup<TransactionStatus>()
        };

        public Task<LookupsDTO> Handle(GetLookupsQuery request, CancellationToken cancellationToken)
        {
            return Task.FromResult(Lookups);
        }

        private static List<LookupDTO<TEnum>> CreateLookup<TEnum>() where TEnum : struct, Enum
        {
            return Enum.GetValues<TEnum>()
                       .Select(value => new LookupDTO<TEnum>(value, value.GetDisplayName()))
                       .ToList();
        }
    }
}
