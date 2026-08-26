#!/usr/bin/env python3
import importlib.util
import unittest
from pathlib import Path

MODULE = Path(__file__).with_name("quality_validators.py")
spec = importlib.util.spec_from_file_location("quality_validators", MODULE)
qv = importlib.util.module_from_spec(spec)
spec.loader.exec_module(qv)


class CSharpMaskTests(unittest.TestCase):
    def test_sql_inside_raw_string_is_masked(self):
        masked, strings = qv.mask_csharp('var sql = """\nselect * from habits where id = @Id\n""";')
        self.assertNotIn("select", masked)
        self.assertEqual(strings[0][1], '"""')

    def test_sql_outside_string_remains_visible(self):
        masked, _ = qv.mask_csharp("select * from habits;")
        self.assertIn("select", masked)

    def test_interpolated_string_is_identified(self):
        _, strings = qv.mask_csharp('$"select * from users where id = {userId}"')
        self.assertIn("$", strings[0][1])


if __name__ == "__main__":
    unittest.main()
