import { readFileSync } from 'node:fs';
import { fileURLToPath } from 'node:url';
import { expect, test } from '@playwright/test';
import { companyMaxLength, rawTextMaxLength, titleMaxLength } from '../../../src/Sift.Web/src/vacancies/vacancyRules';

// The web client validates before sending, so its limits must not drift from the contract.
const openApiPath = fileURLToPath(new URL('../../../api/openapi/openapi.yaml', import.meta.url));

function contractMaxLengths(schema: string): Record<string, number> {
  const lines = readFileSync(openApiPath, 'utf8').split(/\r?\n/);
  const start = lines.indexOf(`    ${schema}:`);
  expect(start, `${schema} is in openapi.yaml`).toBeGreaterThanOrEqual(0);

  const result: Record<string, number> = {};
  let property: string | null = null;

  for (const line of lines.slice(start + 1)) {
    if (/^ {0,4}\S/.test(line)) break;

    const name = /^ {8}(\w+):\s*$/.exec(line);
    if (name) property = name[1];

    const max = /^ {10}maxLength: (\d+)\s*$/.exec(line);
    if (max && property) result[property] = Number(max[1]);
  }

  return result;
}

test('the web vacancy limits match the contract', () => {
  expect({ title: titleMaxLength, company: companyMaxLength, rawText: rawTextMaxLength }).toEqual(
    contractMaxLengths('CreateVacancyRequest'),
  );
});
