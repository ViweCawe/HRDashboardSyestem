"""Test the setup script's safety gates using command doubles, without SQL Server."""
import json
import os
from pathlib import Path
import shutil
import subprocess
import tempfile
import unittest

SOURCE = Path(__file__).resolve().parents[2] / 'scripts/setup-hr-database.sh'
STUB = r'''#!/usr/bin/env python3
import json,os,sys
from pathlib import Path
name=Path(sys.argv[0]).name
args=sys.argv[1:]
with open(os.environ['HR_MOCK_LOG'],'a') as f:
 f.write(json.dumps({'command':name,'args':args,'testConnection':os.getenv('HR_TEST_CONNECTION'),'noPathConversion':os.getenv('MSYS_NO_PATHCONV')})+'\n')
if name=='SqlLocalDB.exe':
 if args[0]=='info':
  if os.getenv('HR_MOCK_HAS_INSTANCE','1')=='0' and not Path(os.environ['HR_MOCK_STATE']).exists(): sys.exit(1)
  print('Name: HRDashboard2025\r\nVersion: '+os.getenv('HR_MOCK_VERSION','17.0.4025.3')+'\r\nState: Stopped')
 if args[0]=='create': Path(os.environ['HR_MOCK_STATE']).touch()
 sys.exit(0)
if args and args[0]=='build' and 'tests/DatabaseValidation/DatabaseValidation.sqlproj' in args:
 p=Path('tests/DatabaseValidation/bin/Debug/DatabaseValidation.dacpac');p.parent.mkdir(parents=True,exist_ok=True);p.touch()
if args[:4]==['tool','run','sqlpackage','--']:
 if os.getenv('MSYS_NO_PATHCONV')!='1' or os.getenv('MSYS2_ARG_CONV_EXCL')!='*': sys.exit(90)
 if '/Action:Publish' in args and any('HrDashboardTests_Local' in a for a in args) and os.getenv('HR_MOCK_FAIL_TEST_PUBLISH')=='1': sys.exit(45)
if args and args[0]=='run' and os.getenv('HR_MOCK_FAIL_CHECKS')=='1': sys.exit(42)
sys.exit(0)
'''


class DatabaseSetupTests(unittest.TestCase):
    def run_setup(self, **settings):
        with tempfile.TemporaryDirectory(prefix='hr-db-script-') as folder:
            root = Path(folder)
            (root / 'scripts').mkdir()
            shutil.copyfile(SOURCE, root / 'scripts/setup-hr-database.sh')
            binary = root / 'bin'
            binary.mkdir()
            for name in ['dotnet', 'SqlLocalDB.exe']:
                path = binary / name
                path.write_text(STUB)
                path.chmod(0o755)
            log = root / 'calls.jsonl'
            env = dict(os.environ, PATH=str(binary) + os.pathsep + os.environ['PATH'],
                       HR_MOCK_LOG=str(log), HR_MOCK_STATE=str(root / 'created'))
            env.update(settings)
            result = subprocess.run(['bash', 'scripts/setup-hr-database.sh'], cwd=root,
                                    env=env, text=True, capture_output=True)
            calls = [json.loads(line) for line in log.read_text().splitlines()]
            return result, calls

    def test_success_uses_named_instance_and_configures_only_hr_connection(self):
        result, calls = self.run_setup()
        self.assertEqual(result.returncode, 0, result.stderr)
        publish = [c for c in calls if '/Action:Publish' in c['args']]
        self.assertEqual(len(publish), 2)
        self.assertTrue(any('Database=HrDashboardTests_Local' in a for a in publish[0]['args']))
        self.assertTrue(any('Database=HrDashboardDev;' in a for a in publish[1]['args']))
        for call in publish:
            self.assertIn('/p:BlockOnPossibleDataLoss=True', call['args'])
            self.assertIn('/p:DropObjectsNotInSource=False', call['args'])
            self.assertTrue(any(r'Server=(localdb)\HRDashboard2025;' in a for a in call['args']))
        configure = [c for c in calls if c['args'][:2] == ['user-secrets', 'set']]
        self.assertEqual(len(configure), 1)
        self.assertEqual(configure[0]['args'][2], 'ConnectionStrings:HrConnection')
        self.assertNotIn('MSSQLLocalDB', json.dumps(calls))
        run_check = next(c for c in calls if c['args'] and c['args'][0] == 'run')
        self.assertIn(r'Server=(localdb)\HRDashboard2025;', run_check['testConnection'])
        self.assertIn('Database=HrDashboardTests_Local;', run_check['testConnection'])

    def test_missing_named_instance_is_created_with_version_17(self):
        result, calls = self.run_setup(HR_MOCK_HAS_INSTANCE='0')
        self.assertEqual(result.returncode, 0, result.stderr)
        self.assertTrue(any(c['command'] == 'SqlLocalDB.exe' and c['args'] == ['create', 'HRDashboard2025', '17.0'] for c in calls))

    def test_existing_old_engine_stops_before_build_or_publication(self):
        result, calls = self.run_setup(HR_MOCK_VERSION='15.0.4382.1')
        self.assertNotEqual(result.returncode, 0)
        self.assertFalse(any(c['command'] == 'dotnet' for c in calls))
        self.assertFalse(any(c['args'][0] in ['create', 'delete', 'stop'] for c in calls))

    def test_failed_sql_checks_never_publish_or_configure_development(self):
        result, calls = self.run_setup(HR_MOCK_FAIL_CHECKS='1')
        self.assertEqual(result.returncode, 42)
        self.assertEqual(len([c for c in calls if '/Action:Publish' in c['args']]), 1)
        self.assertFalse(any(c['args'][:2] == ['user-secrets', 'set'] for c in calls))
        self.assertFalse(any('Database=HrDashboardDev;' in a for c in calls for a in c['args']))

    def test_failed_test_publication_never_runs_checks_or_development_setup(self):
        result, calls = self.run_setup(HR_MOCK_FAIL_TEST_PUBLISH='1')
        self.assertEqual(result.returncode, 45)
        self.assertFalse(any(c['args'] and c['args'][0] == 'run' for c in calls))
        self.assertFalse(any(c['args'][:2] == ['user-secrets', 'set'] for c in calls))


if __name__ == '__main__':
    unittest.main()
