import * as assert from 'assert';
import { hasJdEfcptPackageReference, discoverJdEfcptProjects } from '../../projectDiscovery';

const CSPROJ_WITH_REFERENCE = `<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>net9.0</TargetFramework>
  </PropertyGroup>
  <ItemGroup>
    <PackageReference Include="JD.Efcpt.Build" Version="1.2.3" />
  </ItemGroup>
</Project>`;

const CSPROJ_WITHOUT_REFERENCE = `<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>net9.0</TargetFramework>
  </PropertyGroup>
  <ItemGroup>
    <PackageReference Include="Newtonsoft.Json" Version="13.0.3" />
  </ItemGroup>
</Project>`;

const CSPROJ_WITH_REFERENCE_REORDERED_ATTRS = `<Project Sdk="Microsoft.NET.Sdk">
  <ItemGroup>
    <PackageReference Version="1.2.3" Include="JD.Efcpt.Build" />
  </ItemGroup>
</Project>`;

describe('projectDiscovery', () => {
  describe('hasJdEfcptPackageReference', () => {
    it('returns true when JD.Efcpt.Build is referenced', () => {
      assert.strictEqual(hasJdEfcptPackageReference(CSPROJ_WITH_REFERENCE), true);
    });

    it('returns false when JD.Efcpt.Build is not referenced', () => {
      assert.strictEqual(hasJdEfcptPackageReference(CSPROJ_WITHOUT_REFERENCE), false);
    });

    it('is tolerant of attribute ordering', () => {
      assert.strictEqual(hasJdEfcptPackageReference(CSPROJ_WITH_REFERENCE_REORDERED_ATTRS), true);
    });

    it('returns false for an empty or malformed file', () => {
      assert.strictEqual(hasJdEfcptPackageReference(''), false);
      assert.strictEqual(hasJdEfcptPackageReference('not xml at all'), false);
    });
  });

  describe('discoverJdEfcptProjects', () => {
    it('filters to only the projects referencing JD.Efcpt.Build', () => {
      const files: Record<string, string> = {
        '/repo/A/A.csproj': CSPROJ_WITH_REFERENCE,
        '/repo/B/B.csproj': CSPROJ_WITHOUT_REFERENCE,
        '/repo/C/C.csproj': CSPROJ_WITH_REFERENCE_REORDERED_ATTRS,
      };
      const result = discoverJdEfcptProjects(Object.keys(files), (p) => files[p]);
      assert.deepStrictEqual(result, ['/repo/A/A.csproj', '/repo/C/C.csproj']);
    });

    it('skips files that fail to read instead of throwing', () => {
      const readFile = (p: string): string => {
        if (p === '/repo/missing.csproj') {
          throw new Error('ENOENT');
        }
        return CSPROJ_WITH_REFERENCE;
      };
      const result = discoverJdEfcptProjects(
        ['/repo/missing.csproj', '/repo/ok.csproj'],
        readFile
      );
      assert.deepStrictEqual(result, ['/repo/ok.csproj']);
    });

    it('returns an empty array when given no candidate paths', () => {
      assert.deepStrictEqual(
        discoverJdEfcptProjects([], () => CSPROJ_WITH_REFERENCE),
        []
      );
    });
  });
});
