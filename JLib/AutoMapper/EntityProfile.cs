﻿using System.Runtime.CompilerServices;
using AutoMapper;
using JLib.Helper;
using Serilog;

namespace JLib.AutoMapper;

public class EntityProfile : Profile
{
    public EntityProfile(ITypeCache cache)
    {
        Log.Debug("        Adding GraphQlDataObject Map");
        foreach (var gdo in cache.All<Types.GraphQlDataObject>()
                     .Where(gdo => gdo is { CommandEntity: not null, HasCustomAutoMapperProfile: false }))
        {
            Log.Verbose("            Mapping from {cmd} to {gdo}", gdo.CommandEntity!.Name, gdo.Name);
            CreateMap(gdo.CommandEntity!.Value, gdo.Value);
        }

        Log.Debug("        Adding GraphQlMutationParameters");
        foreach (var gmp in cache.All<Types.GraphQlMutationParameter>()
                     .Where(gdo => gdo is { CommandEntity: not null, HasCustomAutoMapperProfile: false }))
        {
            Log.Verbose("            Mapping from {cmd} to {gdo}", gmp.CommandEntity!.Name, gmp.CommandEntity.Name);
            var ceProps = gmp.CommandEntity!.Value.GetProperties();
            var gmpProps = gmp.Value.GetProperties();


            var mapper = CreateMap(gmp.Value, gmp.CommandEntity!.Value);

            // remove all properties which are missing in the mutation parameter and are not required
            var propsToIgnore = ceProps
                .ExceptBy(gmpProps.Select(pGmp => pGmp.Name), pCe => pCe.Name)
                .Where(ceProp => !ceProp.HasCustomAttribute<RequiredMemberAttribute>());
            foreach (var prop in propsToIgnore)
            {
                mapper.ForMember(prop.Name, o => o.Ignore());
                Log.Verbose("            Adding {propName} to the ignore list", prop.Name);
            }
        }
    }
}