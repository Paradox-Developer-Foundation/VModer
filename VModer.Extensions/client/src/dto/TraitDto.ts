import type { DocumentRange } from '../../src/types/DocumentRange';
import type { FileOrigin } from './FileOrigin';
import type { TraitType } from './TraitType';

export interface TraitDto {
  FilePath: string;
  Name: string;
  LocalizedName: string;
  Modifiers: string;
  FileOrigin: FileOrigin;
  Type: TraitType;
  Description?: string;
  Position: DocumentRange;
  /** file:// 形式的 Uri */
  IconPath?: string;
}

export * from './TraitDto';
export * from './FileOrigin';
export * from './TraitType';