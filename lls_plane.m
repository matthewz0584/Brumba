function coefs = lls_plane(points)
%    points
    A = [[points(:, 1)' * points(:, 1), points(:, 1)' * points(:, 2), points(:, 1)' * points(:, 3)]
         [points(:, 2)' * points(:, 1), points(:, 2)' * points(:, 2), points(:, 2)' * points(:, 3)]
         [points(:, 3)' * points(:, 1), points(:, 3)' * points(:, 2), points(:, 3)' * points(:, 3)]]
    Ainv = inv(A)
    B = [sum(points(:, 1));
         sum(points(:, 2));
         sum(points(:, 3));]
    n = inv(A) * B
    d = -sum(points * n) / size(points)(1)
%    coefs = [n; -1];
    coefs = [n; d]
endfunction