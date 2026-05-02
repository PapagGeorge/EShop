import { ReactNode } from 'react';
import { Link, useNavigate } from 'react-router-dom';
import { useAuthStore } from '../store/authStore';
import { useCartStore } from '../store/cartStore';

export default function Layout({ children }: { children: ReactNode }) {
  const { user, logout, isAuthenticated } = useAuthStore();
  const itemCount = useCartStore(s => s.items.reduce((sum, i) => sum + i.quantity, 0));
  const navigate = useNavigate();

  const handleLogout = () => {
    logout();
    navigate('/login');
  };

  return (
    <div className="min-h-screen bg-gray-50">
      <nav className="bg-white shadow-sm border-b">
        <div className="max-w-6xl mx-auto px-4 py-3 flex items-center justify-between">
          <Link to="/" className="text-xl font-bold text-indigo-600">EShop</Link>

          <div className="flex items-center gap-6">
            <Link to="/" className="text-gray-600 hover:text-indigo-600 text-sm">
              Products
            </Link>

            {isAuthenticated() ? (
              <>
                <Link to="/orders" className="text-gray-600 hover:text-indigo-600 text-sm">
                  Orders
                </Link>
                <Link to="/cart" className="relative text-gray-600 hover:text-indigo-600 text-sm">
                  Cart
                  {itemCount > 0 && (
                    <span className="absolute -top-2 -right-3 bg-indigo-600 text-white text-xs rounded-full w-4 h-4 flex items-center justify-center">
                      {itemCount}
                    </span>
                  )}
                </Link>
                <span className="text-sm text-gray-400">{user?.fullName}</span>
                <button
                  onClick={handleLogout}
                  className="text-sm text-red-500 hover:text-red-700"
                >
                  Logout
                </button>
              </>
            ) : (
              <Link to="/login" className="text-gray-600 hover:text-indigo-600 text-sm">
                Login
              </Link>
            )}
          </div>
        </div>
      </nav>

      <main className="max-w-6xl mx-auto px-4 py-8">{children}</main>
    </div>
  );
}
