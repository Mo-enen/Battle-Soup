using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;


[System.Serializable]
public class Matrix2x2 {

	public float m00, m01, m10, m11;

	public static float MATRIX_EPSILON = 1e-6f;

	public Matrix2x2 () {
		SetValue(1, 0, 0, 1);
	}

	public Matrix2x2 (float m00, float m01, float m10, float m11) {
		SetValue(m00, m01, m10, m11);
	}

	public Matrix2x2 (Matrix2x2 m) {
		SetValue(m[0, 0], m[0, 1], m[1, 0], m[1, 1]);
	}

	public Matrix2x2 (float m00, float m11) {
		// Diagonal
		SetValue(m00, 0, 0, m11);
	}

	public void LoadIdentity () {
		SetValue(1, 0, 0, 1);
	}

	public void SetValue (float m00, float m01, float m10, float m11) {
		this[0, 0] = m00;
		this[0, 1] = m01;
		this[1, 0] = m10;
		this[1, 1] = m11;
	}

	public void SetValue (float m00, float m11) {
		SetValue(m00, 0, 0, m11);
	}

	public void SetValue (Matrix2x2 m) {
		SetValue(m[0, 0], m[0, 1], m[1, 0], m[1, 1]);
	}

	public void SetValue (float value) {
		SetValue(value, value, value, value);
	}

	public void Normalize () {
		for (int row = 0; row < 2; row++) {
			float l = 0;
			for (int column = 0; column < 2; column++) {
				l += this[row, column] * this[row, column];
			}

			l = Mathf.Sqrt(l);

			for (int column = 0; column < 2; column++) {
				this[row, column] /= l;
			}
		}
	}

	public float Determinant () {
		return this[0, 0] * this[1, 1] - this[0, 1] * this[1, 0];
	}

	public Matrix2x2 Transpose () {
		return new Matrix2x2(this[0, 0], this[1, 0], this[0, 1], this[1, 1]);
	}

	public Matrix2x2 Inverse () {
		float det = Determinant();
		return new Matrix2x2(this[1, 1] / det, -this[0, 1] / det, -this[1, 0] / det, this[0, 0] / det);
	}

	public Matrix2x2 Cofactor () {
		return new Matrix2x2(this[1, 1], -this[1, 0], -this[0, 1], this[0, 0]);
	}

	public float FrobeniusInnerProduct (Matrix2x2 m) {
		float prod = 0;
		for (int i = 0; i < 2; i++) {
			for (int j = 0; j < 2; j++) {
				prod += this[i, j] * m[i, j];
			}
		}
		return prod;
	}

	/// <summary>
	/// Singular Value Decomposition
	/// </summary>
	/// <param name="w">Returns rotation matrix</param>
	/// <param name="e">Returns sigma matrix</param>
	/// <param name="v">Returns (not transposed)</param>
	public void SVD (ref Matrix2x2 w, ref Matrix2x2 e, ref Matrix2x2 v) {
		// If it is diagonal, SVD is trivial
		if (Mathf.Abs(this[1, 0] - this[0, 1]) < MATRIX_EPSILON && Mathf.Abs(this[1, 0]) < MATRIX_EPSILON) {
			w.SetValue(this[0, 0] < 0 ? -1 : 1, 0, 0, this[1, 1] < 0 ? -1 : 1);
			e.SetValue(Mathf.Abs(this[0, 0]), Mathf.Abs(this[1, 1]));
			v.LoadIdentity();
		}

		// Otherwise, we need to compute A^T*A
		else {
			float j = this[0, 0] * this[0, 0] + this[1, 0] * this[1, 0],
				k = this[0, 1] * this[0, 1] + this[1, 1] * this[1, 1],
				v_c = this[0, 0] * this[0, 1] + this[1, 0] * this[1, 1];
			// Check to see if A^T*A is diagonal
			if (Mathf.Abs(v_c) < MATRIX_EPSILON) {
				float s1 = Mathf.Sqrt(j), s2 = Mathf.Abs(j - k) < MATRIX_EPSILON ? s1 : Mathf.Sqrt(k);
				e.SetValue(s1, s2);
				v.LoadIdentity();
				w.SetValue(this[0, 0] / s1, this[0, 1] / s2, this[1, 0] / s1, this[1, 1] / s2);
			}
			// Otherwise, solve quadratic for eigenvalues
			else {
				float jmk = j - k,
					jpk = j + k,
					root = Mathf.Sqrt(jmk * jmk + 4 * v_c * v_c),
					eig = (jpk + root) / 2,
					s1 = Mathf.Sqrt(eig),
					s2 = Mathf.Abs(root) < MATRIX_EPSILON ? s1 : Mathf.Sqrt((jpk - root) / 2);

				e.SetValue(s1, s2);

				// Use eigenvectors of A^T*A as V
				float v_s = eig - j, len = Mathf.Sqrt(v_s * v_s + v_c * v_c);
				v_c /= len;
				v_s /= len;
				v.SetValue(v_c, -v_s, v_s, v_c);
				// Compute w matrix as Av/s
				w.SetValue(
					(this[0, 0] * v_c + this[0, 1] * v_s) / s1,
					(this[0, 1] * v_c - this[0, 0] * v_s) / s2,
					(this[1, 0] * v_c + this[1, 1] * v_s) / s1,
					(this[1, 1] * v_c - this[1, 0] * v_s) / s2
				);
			}
		}
	}

	//DIAGONAL MATRIX OPERATIONS
	//Matrix * Matrix
	public void DiagProduct (Vector2 v) {
		for (int i = 0; i < 2; i++) {
			for (int j = 0; j < 2; j++)
				this[i, j] *= v[i];
		}
	}

	//Matrix * Matrix^-1
	public void DiagProductInv (Vector2 v) {
		for (int i = 0; i < 2; i++) {
			for (int j = 0; j < 2; j++)
				this[i, j] /= v[i];
		}
	}

	//Matrix - Matrix
	public void DiagDifference (float c) {
		for (int i = 0; i < 2; i++)
			this[i, i] -= c;
	}

	public void DiagDifference (Vector2 v) {
		for (int i = 0; i < 2; i++)
			this[i, i] -= v[i];
	}

	//Matrix + Matrix
	public void DiagSum (float c) {
		for (int i = 0; i < 2; i++)
			this[i, i] += c;
	}
	public void DiagSum (Vector2 v) {
		for (int i = 0; i < 2; i++)
			this[i, i] += v[i];
	}

	static Matrix2x2 Identity () {
		return new Matrix2x2(1, 0, 0, 1);
	}

	// Array subscripts
	public float this[int row, int column] {
		get {
			if (row == 0 && column == 0) return m00;
			else if (row == 0 && column == 1) return m01;
			else if (row == 1 && column == 0) return m10;
			else if (row == 1 && column == 1) return m11;
			else throw new ArgumentOutOfRangeException();
		}
		set {
			if (row == 0 && column == 0) { m00 = value; } else if (row == 0 && column == 1) m01 = value;
			else if (row == 1 && column == 0) m10 = value;
			else if (row == 1 && column == 1) m11 = value;
			else throw new ArgumentOutOfRangeException();
		}
	}

	public float this[int index] {
		get {
			if (index == 0) return m00;
			else if (index == 1) return m01;
			else if (index == 2) return m10;
			else if (index == 3) return m11;
			else throw new ArgumentOutOfRangeException();
		}
		set {
			if (index == 0) { m00 = value; } else if (index == 1) m01 = value;
			else if (index == 2) m10 = value;
			else if (index == 3) m11 = value;
			else throw new ArgumentOutOfRangeException();
		}
	}

	// Matrix - Scalar overloads
	public static Matrix2x2 operator + (Matrix2x2 l, float r) {
		Matrix2x2 result = new Matrix2x2(l);
		for (int index = 0; index < 4; index++) {
			result[index] += r;
		}
		return result;
	}

	public static Matrix2x2 operator + (float l, Matrix2x2 r) {
		Matrix2x2 result = new Matrix2x2(r);
		for (int index = 0; index < 4; index++) {
			result[index] += l;
		}
		return result;
	}

	public static Matrix2x2 operator - (Matrix2x2 l, float r) {
		Matrix2x2 result = new Matrix2x2(l);
		for (int index = 0; index < 4; index++) {
			result[index] -= r;
		}
		return result;
	}

	public static Matrix2x2 operator * (Matrix2x2 l, float r) {
		Matrix2x2 result = new Matrix2x2(l);
		for (int index = 0; index < 4; index++) {
			result[index] *= r;
		}
		return result;
	}

	public static Matrix2x2 operator * (float l, Matrix2x2 r) {
		Matrix2x2 result = new Matrix2x2(r);
		for (int index = 0; index < 4; index++) {
			result[index] *= l;
		}
		return result;
	}

	public static Matrix2x2 operator / (Matrix2x2 l, float r) {
		Matrix2x2 result = new Matrix2x2(l);
		for (int index = 0; index < 4; index++) {
			result[index] /= r;
		}
		return result;
	}

	// Matrix - Matrix overloads
	public static Matrix2x2 operator + (Matrix2x2 l, Matrix2x2 r) {
		Matrix2x2 result = new Matrix2x2(l);
		for (int row = 0; row < 2; row++) {
			for (int column = 0; column < 2; column++) {
				result[row, column] += r[row, column];
			}
		}
		return result;
	}

	public static Matrix2x2 operator - (Matrix2x2 l, Matrix2x2 r) {
		Matrix2x2 result = new Matrix2x2(l);
		for (int row = 0; row < 2; row++) {
			for (int column = 0; column < 2; column++) {
				result[row, column] -= r[row, column];
			}
		}
		return result;
	}

	public static Matrix2x2 operator * (Matrix2x2 l, Matrix2x2 r) {
		Matrix2x2 result = new Matrix2x2(l);
		for (int row = 0; row < 2; row++) {
			for (int column = 0; column < 2; column++) {

				result[row, column] = l[row, 0] * r[0, column];
				for (int i = 1; i < 2; i++) {
					result[row, column] += l[row, i] * r[i, column];
				}

			}
		}
		return result;
	}

	// Matrix - Vector Overloads
	public static Vector2 operator * (Matrix2x2 l, Vector2 r) {
		return new Vector2(
				l[0, 0] * r[0] + l[0, 1] * r[1],
				l[1, 0] * r[0] + l[1, 1] * r[1]
			);
	}

	public override string ToString () {
		string str = "[\n";
		for (int row = 0; row < 2; row++) {
			str += "[";
			for (int column = 0; column < 2; column++) {
				str += this[row, column] + ", ";
			}
			str += "]\n";
		}
		str += "]";

		return str;
	}

}