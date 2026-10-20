using AutoMapper;

namespace TransactionDisputePortal.Application.Common.Mappings;

public interface IMapFrom<TEntity>
{
    void Mapping(Profile profile) => profile.CreateMap(typeof(TEntity), GetType());
}
