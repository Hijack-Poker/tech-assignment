import { validate } from 'class-validator';
import { plainToInstance } from 'class-transformer';
import { AwardPointsDto } from '../src/points/dto/award-points.dto';

function createDto(overrides: Partial<Record<string, unknown>> = {}): AwardPointsDto {
  return plainToInstance(AwardPointsDto, {
    tableId: 1,
    tableStakes: '1/2',
    bigBlind: 2.0,
    handId: 'hand-001',
    ...overrides,
  });
}

describe('AwardPointsDto', () => {
  it('passes validation with valid data', async () => {
    const dto = createDto();
    const errors = await validate(dto);
    expect(errors).toHaveLength(0);
  });

  it('rejects missing tableId', async () => {
    const dto = createDto({ tableId: undefined });
    const errors = await validate(dto);
    expect(errors.length).toBeGreaterThan(0);
    expect(errors.some((e) => e.property === 'tableId')).toBe(true);
  });

  it('rejects non-positive tableId', async () => {
    const dto = createDto({ tableId: 0 });
    const errors = await validate(dto);
    expect(errors.some((e) => e.property === 'tableId')).toBe(true);
  });

  it('rejects negative tableId', async () => {
    const dto = createDto({ tableId: -1 });
    const errors = await validate(dto);
    expect(errors.some((e) => e.property === 'tableId')).toBe(true);
  });

  it('rejects empty tableStakes', async () => {
    const dto = createDto({ tableStakes: '' });
    const errors = await validate(dto);
    expect(errors.some((e) => e.property === 'tableStakes')).toBe(true);
  });

  it('rejects missing bigBlind', async () => {
    const dto = createDto({ bigBlind: undefined });
    const errors = await validate(dto);
    expect(errors.some((e) => e.property === 'bigBlind')).toBe(true);
  });

  it('rejects non-positive bigBlind', async () => {
    const dto = createDto({ bigBlind: 0 });
    const errors = await validate(dto);
    expect(errors.some((e) => e.property === 'bigBlind')).toBe(true);
  });

  it('rejects empty handId', async () => {
    const dto = createDto({ handId: '' });
    const errors = await validate(dto);
    expect(errors.some((e) => e.property === 'handId')).toBe(true);
  });

  it('rejects missing handId', async () => {
    const dto = createDto({ handId: undefined });
    const errors = await validate(dto);
    expect(errors.some((e) => e.property === 'handId')).toBe(true);
  });

  it('accepts decimal bigBlind values', async () => {
    const dto = createDto({ bigBlind: 0.25 });
    const errors = await validate(dto);
    expect(errors).toHaveLength(0);
  });
});
