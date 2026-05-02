import { useState } from 'react';
import { useNavigate } from 'react-router-dom';
import { useCartStore } from '../store/cartStore';
import { useAuthStore } from '../store/authStore';
import { createOrder } from '../api/ordersApi';
import { Address } from '../types';

const emptyAddress: Address = { street: '', city: '', zipCode: '', country: '' };

export default function CartPage() {
  const { items, removeItem, updateQuantity, clear } = useCartStore();
  const total = useCartStore(s => s.items.reduce((sum, i) => sum + i.product.price * i.quantity, 0));
  const isAuthenticated = useAuthStore(s => s.isAuthenticated());
  const [address, setAddress] = useState<Address>(emptyAddress);
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState('');
  const navigate = useNavigate();

  const handlePlaceOrder = async () => {
    setLoading(true);
    setError('');
    try {
      await createOrder(
        address,
        items.map(i => ({ productId: i.product.id, quantity: i.quantity }))
      );
      clear();
      navigate('/orders');
    } catch {
      setError('Failed to place order. Please try again.');
    } finally {
      setLoading(false);
    }
  };

  const isAddressComplete =
    address.street && address.city && address.zipCode && address.country;

  if (items.length === 0) {
    return (
      <div className="text-center py-16">
        <p className="text-gray-500 text-lg mb-4">Your cart is empty.</p>
        <button
          onClick={() => navigate('/')}
          className="text-indigo-600 hover:underline text-sm"
        >
          Browse products
        </button>
      </div>
    );
  }

  return (
    <div className="grid grid-cols-1 lg:grid-cols-3 gap-8">
      {/* Cart items */}
      <div className="lg:col-span-2 space-y-3">
        <h1 className="text-2xl font-bold mb-2">Your Cart</h1>
        {items.map(item => (
          <div
            key={item.product.id}
            className="bg-white rounded-xl shadow p-4 flex items-center gap-4"
          >
            <div className="flex-1 min-w-0">
              <p className="font-semibold truncate">{item.product.name}</p>
              <p className="text-sm text-gray-400">€{item.product.price.toFixed(2)} each</p>
            </div>
            <div className="flex items-center gap-2">
              <button
                onClick={() => updateQuantity(item.product.id, item.quantity - 1)}
                className="w-7 h-7 rounded-full border flex items-center justify-center hover:bg-gray-100 text-lg leading-none"
              >
                −
              </button>
              <span className="w-6 text-center text-sm">{item.quantity}</span>
              <button
                onClick={() => updateQuantity(item.product.id, item.quantity + 1)}
                className="w-7 h-7 rounded-full border flex items-center justify-center hover:bg-gray-100 text-lg leading-none"
              >
                +
              </button>
            </div>
            <p className="w-20 text-right font-semibold text-sm">
              €{(item.product.price * item.quantity).toFixed(2)}
            </p>
            <button
              onClick={() => removeItem(item.product.id)}
              className="text-red-400 hover:text-red-600 text-sm"
            >
              Remove
            </button>
          </div>
        ))}
      </div>

      {/* Order summary */}
      <div className="bg-white rounded-xl shadow p-6 h-fit space-y-4">
        <h2 className="text-lg font-bold">Order Summary</h2>
        <div className="flex justify-between font-bold text-lg border-t pt-3">
          <span>Total</span>
          <span>€{total.toFixed(2)}</span>
        </div>

        {isAuthenticated ? (
          <>
            <h3 className="font-semibold text-sm text-gray-700 pt-2">Shipping Address</h3>
            {(
              [
                { key: 'street', placeholder: 'Street' },
                { key: 'city', placeholder: 'City' },
                { key: 'zipCode', placeholder: 'ZIP Code' },
                { key: 'country', placeholder: 'Country' },
              ] as { key: keyof Address; placeholder: string }[]
            ).map(({ key, placeholder }) => (
              <input
                key={key}
                placeholder={placeholder}
                value={address[key]}
                onChange={e => setAddress({ ...address, [key]: e.target.value })}
                className="w-full border rounded-lg px-3 py-2 text-sm focus:outline-none focus:ring-2 focus:ring-indigo-500"
              />
            ))}

            {error && <p className="text-red-500 text-sm">{error}</p>}

            <button
              onClick={handlePlaceOrder}
              disabled={loading || !isAddressComplete}
              className="w-full bg-indigo-600 text-white py-2 rounded-lg hover:bg-indigo-700 disabled:opacity-50 transition-colors"
            >
              {loading ? 'Placing order...' : 'Place Order'}
            </button>
          </>
        ) : (
          <button
            onClick={() => navigate('/login')}
            className="w-full bg-indigo-600 text-white py-2 rounded-lg hover:bg-indigo-700 transition-colors"
          >
            Login to Checkout
          </button>
        )}
      </div>
    </div>
  );
}
