import type { DocumentRange } from '../../src/types/DocumentRange';
import type { FileOrigin } from './FileOrigin';
import type { TraitKind } from './TraitKind';
import type { TraitType } from './TraitType';

export interface TraitDto {
  FilePath: string;
  Name: string;
  LocalizedName: string;
  Modifiers: string;
  FileOrigin: FileOrigin;
  GeneralType: TraitType;
  Description?: string;
  Position: DocumentRange;
  /** file:// 形式的 Uri */ 
  IconPath?: string;
  Type: TraitKind
}

export * from './TraitDto';
export * from './FileOrigin';
export * from './TraitType';
export * from './TraitKind';