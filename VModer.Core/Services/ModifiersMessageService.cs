﻿using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;
using CsvHelper;
using MethodTimer;
using VModer.Core.Dto;
using VModer.Core.Models.Modifiers;
using VModer.Core.Services.GameResource;
using VModer.Core.Services.GameResource.Localization;
using VModer.Core.Services.GameResource.Modifiers;

namespace VModer.Core.Services;

public sealed class ModifiersMessageService
{
    private readonly ModifierDto[] _modifierDto;
    private static readonly char[] TrimChars = [':', '：'];

    [Time]
    public ModifiersMessageService(
        BuildingsService buildingsService,
        OreService oreService,
        ModifierService modifierService,
        LocalizationFormatService localizationFormatService,
        IdeologiesService ideologiesService,
        UnitService unitService,
        OperationsService operationsService
    )
    {
        string filePtah = Path.Combine(App.AssetsFolder, "Modifiers.csv");

        using var csv = new CsvReader(File.OpenText(filePtah), CultureInfo.InvariantCulture);

        var modifiers = new List<ModifierMessage>();
        csv.Read();
        csv.ReadHeader();
        while (csv.Read())
        {
            string name = csv.GetField<string>("Name") ?? string.Empty;
            string[] categories = csv.GetField<string>("Categories")?.Split(';') ?? [];
            var modifierMessage = new ModifierMessage(name, categories);
            modifiers.Add(modifierMessage);
        }

        ReadDynamicModifiers(
            modifiers,
            buildingsService,
            oreService,
            ideologiesService,
            unitService,
            operationsService
        );

        _modifierDto = modifiers
            .Select(message => new ModifierDto
            {
                Name = message.Name,
                Categories = message.Categories,
                LocalizedName = string.Join(
                        string.Empty,
                        localizationFormatService
                            .GetFormatTextInfo(modifierService.GetLocalizationName(message.Name))
                            .Select(info => info.DisplayText)
                    )
                    .TrimEnd(TrimChars)
            })
            .ToArray();
    }

    private static void ReadDynamicModifiers(
        List<ModifierMessage> modifiers,
        BuildingsService buildingsService,
        OreService oreService,
        IdeologiesService ideologiesService,
        UnitService unitService,
        OperationsService operationsService
    )
    {
        string filePtah = Path.Combine(App.AssetsFolder, "DynamicModifiers.csv");
        using var dynamicCsv = new CsvReader(File.OpenText(filePtah), CultureInfo.InvariantCulture);
        dynamicCsv.Read();
        dynamicCsv.ReadHeader();

        while (dynamicCsv.Read())
        {
            string name = dynamicCsv.GetField<string>("Name") ?? string.Empty;
            string[] categories = dynamicCsv.GetField<string>("Categories")?.Split(';') ?? [];

            if (name.Contains("<Building>"))
            {
                foreach (var building in buildingsService.All)
                {
                    modifiers.Add(new ModifierMessage(name.Replace("<Building>", building.Name), categories));
                }
            }
            else if (name.Contains("<Resource>"))
            {
                foreach (string oreName in oreService.All)
                {
                    modifiers.Add(new ModifierMessage(name.Replace("<Resource>", oreName), categories));
                }
            }
            else if (name.Contains("<Ideology>"))
            {
                foreach (string ideologyName in ideologiesService.All)
                {
                    modifiers.Add(new ModifierMessage(name.Replace("<Ideology>", ideologyName), categories));
                }
            }
            else if (name.Contains("<Unit>"))
            {
                foreach (string unitName in unitService.All)
                {
                    modifiers.Add(new ModifierMessage(name.Replace("<Unit>", unitName), categories));
                }
            }
            else if (name.Contains("<Operation>"))
            {
                foreach (string operationName in operationsService.OperationNames)
                {
                    modifiers.Add(
                        new ModifierMessage(name.Replace("<Operation>", operationName), categories)
                    );
                }
            }
            else
            {
                var modifierMessage = new ModifierMessage(name, categories);
                modifiers.Add(modifierMessage);
            }
        }
    }

    [Time]
    public JsonDocument GetModifierJson()
    {
        return JsonDocument.Parse(
            JsonSerializer.Serialize(_modifierDto, ModifierSerializerContext.Default.ModifierDtoArray)
        );
    }
}

[JsonSourceGenerationOptions]
[JsonSerializable(typeof(ModifierDto[]))]
internal partial class ModifierSerializerContext : JsonSerializerContext;
