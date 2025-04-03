/**
 * 与服务端中的 TraitType 枚举对应
 */
export enum TraitType {
  None = 0,
  Land = 1,
  Navy = 2,
  CorpsCommander = 4,
  FieldMarshal = 8,
  Operative = 16,
  All = Land | Navy | CorpsCommander | FieldMarshal | Operative,
}

export function getTraitTypeValues(): TraitType[] {
  return [
    TraitType.Land,
    TraitType.Navy,
    TraitType.CorpsCommander,
    TraitType.FieldMarshal,
    TraitType.Operative,
  ];
}
/**
 * 检查枚举值是否包含指定标志
 */
export function hasFlag(value: TraitType, flag: TraitType): boolean {
  return (value & flag) !== 0;
}

/**
 * 获取枚举值中设置的所有标志
 * 返回各个单独标志的数组
 */
export function getFlags(value: TraitType): TraitType[] {
  const result: TraitType[] = [];

  // 检查每个可能的标志位 (跳过 None 和 All)
  if (hasFlag(value, TraitType.Land)) {
    result.push(TraitType.Land);
  }
  if (hasFlag(value, TraitType.Navy)) {
    result.push(TraitType.Navy);
  }
  if (hasFlag(value, TraitType.CorpsCommander)) {
    result.push(TraitType.CorpsCommander);
  }
  if (hasFlag(value, TraitType.FieldMarshal)) {
    result.push(TraitType.FieldMarshal);
  }
  if (hasFlag(value, TraitType.Operative)) {
    result.push(TraitType.Operative);
  }

  return result.length > 0 ? result : [TraitType.None];
}
