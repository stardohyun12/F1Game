import unittest
import numpy as np
from vertex_position_audit import compare_positions

class PositionAuditTests(unittest.TestCase):
    def test_permutation_and_uv_seam_duplicates(self):
        self.assertTrue(compare_positions([[0,0,0],[1,2,3]],[[1,2,3],[0,0,0],[0,0,0]])['pass'])
    def test_rounding_boundary_order_does_not_define_correspondence(self):
        a=[[.0000499,0,0],[.0000501,1,0]]
        b=[[.0000501,0,0],[.0000499,1,0]]
        r=compare_positions(a,b)
        self.assertFalse(r['legacy_sorted_rounded_pass']);self.assertTrue(r['pass'])
    def test_difference_over_absolute_tolerance_fails(self):
        self.assertFalse(compare_positions([[1,2,3]],[[1.00010001,2,3]])['pass'])
    def test_in_tolerance_at_cell_boundary(self):
        self.assertTrue(compare_positions([[-.0000001,0,0]],[[.0000001,0,0]])['pass'])
    def test_reverse_check_rejects_extra_position(self):
        self.assertFalse(compare_positions([[0,0,0]],[[0,0,0],[1,0,0]])['pass'])
    def test_missing_position_fails(self):
        self.assertFalse(compare_positions([[0,0,0],[1,0,0]],[[0,0,0]])['pass'])
    def test_large_coordinates_do_not_get_relative_tolerance(self):
        self.assertFalse(compare_positions([[10000,0,0]],[[10000.001,0,0]])['pass'])
    def test_nonfinite_rejected(self):
        self.assertFalse(compare_positions([[np.nan,0,0]],[[0,0,0]])['pass'])
    def test_empty_rejected(self):
        self.assertFalse(compare_positions(np.empty((0,3)),[[0,0,0]])['pass'])

if __name__=='__main__':unittest.main()
